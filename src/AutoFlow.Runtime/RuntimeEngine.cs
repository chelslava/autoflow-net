// Этот код нужен для выполнения workflow-документа через runtime и последовательного запуска шагов.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Runtime;

public sealed class RuntimeEngine : IRuntimeEngine
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KeywordExecutor _keywordExecutor;
    private readonly ILogger<RuntimeEngine> _logger;

    public RuntimeEngine(
        IServiceProvider serviceProvider,
        KeywordExecutor keywordExecutor,
        ILogger<RuntimeEngine> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _keywordExecutor = keywordExecutor ?? throw new ArgumentNullException(nameof(keywordExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RunResult> ExecuteAsync(
        WorkflowDocument document,
        RuntimeLaunchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);

        var startedAt = DateTimeOffset.UtcNow;

        var runResult = new RunResult
        {
            WorkflowName = document.Name,
            StartedAtUtc = startedAt,
            Status = ExecutionStatus.Passed
        };

        var variables = document.Variables
            .Concat(options.Variables)
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last().Value, StringComparer.OrdinalIgnoreCase);

        var context = new ExecutionContext(_serviceProvider, variables);

        if (!document.Tasks.TryGetValue("main", out var mainTask))
            throw new InvalidOperationException("В документе не найдена задача 'main'.");

        foreach (var node in mainTask.Steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node is not StepNode stepNode)
                throw new NotSupportedException($"Узел типа '{node.GetType().Name}' пока не поддерживается в стартовом runtime.");

            var stepStartedAt = DateTimeOffset.UtcNow;
            var stepResult = new StepExecutionResult
            {
                StepId = stepNode.Id,
                KeywordName = stepNode.Uses,
                StartedAtUtc = stepStartedAt,
                Status = ExecutionStatus.Passed
            };

            try
            {
                var resolvedArgs = VariableResolver.ResolveObject(stepNode.With, context);

                var keywordResult = await _keywordExecutor.ExecuteAsync(
                    context,
                    stepNode.Id,
                    stepNode.Uses,
                    resolvedArgs,
                    cancellationToken).ConfigureAwait(false);

                stepResult.Status = keywordResult.IsSuccess
                    ? ExecutionStatus.Passed
                    : ExecutionStatus.Failed;

                stepResult.Outputs = keywordResult.Outputs;
                stepResult.ErrorMessage = keywordResult.ErrorMessage;

                stepResult.Logs.AddRange(keywordResult.Logs);

                context.SetStepResult(stepNode.Id, keywordResult.Outputs);

                if (!string.IsNullOrWhiteSpace(stepNode.SaveAs))
                    context.SetVariable(stepNode.SaveAs!, keywordResult.Outputs);

                if (!keywordResult.IsSuccess && !stepNode.ContinueOnError)
                {
                    runResult.Status = ExecutionStatus.Failed;
                    stepResult.FinishedAtUtc = DateTimeOffset.UtcNow;
                    runResult.Steps.Add(stepResult);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении шага {StepId}.", stepNode.Id);

                stepResult.Status = ExecutionStatus.Failed;
                stepResult.ErrorMessage = ex.Message;
                runResult.Status = ExecutionStatus.Failed;

                if (!stepNode.ContinueOnError)
                {
                    stepResult.FinishedAtUtc = DateTimeOffset.UtcNow;
                    runResult.Steps.Add(stepResult);
                    break;
                }
            }

            stepResult.FinishedAtUtc = DateTimeOffset.UtcNow;
            runResult.Steps.Add(stepResult);
        }

        runResult.FinishedAtUtc = DateTimeOffset.UtcNow;

        if (runResult.Status == ExecutionStatus.Passed &&
            runResult.Steps.Any(x => x.Status == ExecutionStatus.Failed))
        {
            runResult.Status = ExecutionStatus.Failed;
        }

        return runResult;
    }
}
