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

        await ExecuteNodes(mainTask.Steps, document, context, runResult, cancellationToken).ConfigureAwait(false);

        runResult.FinishedAtUtc = DateTimeOffset.UtcNow;

        return runResult;
    }

    private async Task ExecuteNodes(
        List<IWorkflowNode> nodes,
        WorkflowDocument document,
        ExecutionContext context,
        RunResult runResult,
        CancellationToken cancellationToken)
    {
        foreach (var node in nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (runResult.Status == ExecutionStatus.Failed)
                break;

            await ExecuteNode(node, document, context, runResult, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteNode(
        IWorkflowNode node,
        WorkflowDocument document,
        ExecutionContext context,
        RunResult runResult,
        CancellationToken cancellationToken)
    {
        switch (node)
        {
            case StepNode stepNode:
                await ExecuteStep(stepNode, context, runResult, cancellationToken).ConfigureAwait(false);
                break;

            case IfNode ifNode:
                await ExecuteIf(ifNode, document, context, runResult, cancellationToken).ConfigureAwait(false);
                break;

            case ForEachNode forEachNode:
                await ExecuteForEach(forEachNode, document, context, runResult, cancellationToken).ConfigureAwait(false);
                break;

            case CallNode callNode:
                await ExecuteCall(callNode, document, context, runResult, cancellationToken).ConfigureAwait(false);
                break;

            case GroupNode groupNode:
                await ExecuteGroup(groupNode, document, context, runResult, cancellationToken).ConfigureAwait(false);
                break;

            default:
                throw new NotSupportedException($"Узел типа '{node.GetType().Name}' не поддерживается.");
        }
    }

    private async Task ExecuteStep(
        StepNode step,
        ExecutionContext context,
        RunResult runResult,
        CancellationToken cancellationToken)
    {
        if (!ShouldExecute(step.When, context))
        {
            _logger.LogInformation("Шаг {StepId} пропущен (condition=false).", step.Id);
            return;
        }

        var stepResult = new StepExecutionResult
        {
            StepId = step.Id,
            KeywordName = step.Uses,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = ExecutionStatus.Passed
        };

        var timeoutMs = ParseTimeout(step.Timeout);
        using var timeoutCts = timeoutMs.HasValue
            ? new CancellationTokenSource(timeoutMs.Value)
            : null;

        using var linkedCts = timeoutCts is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
            : null;

        var effectiveToken = linkedCts?.Token ?? cancellationToken;

        var maxAttempts = step.Retry?.Attempts ?? 1;
        var delayMs = ParseDelay(step.Retry?.Delay);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var resolvedArgs = VariableResolver.ResolveObject(step.With, context);

                var keywordResult = await _keywordExecutor.ExecuteAsync(
                    context,
                    step.Id,
                    step.Uses,
                    resolvedArgs,
                    effectiveToken).ConfigureAwait(false);

                stepResult.Status = keywordResult.IsSuccess
                    ? ExecutionStatus.Passed
                    : ExecutionStatus.Failed;

                stepResult.Outputs = keywordResult.Outputs;
                stepResult.ErrorMessage = keywordResult.ErrorMessage;
                stepResult.Logs.AddRange(keywordResult.Logs);

                if (keywordResult.IsSuccess)
                {
                    context.SetStepResult(step.Id, keywordResult.Outputs);

                    if (step.SaveAs is not null)
                    {
                        foreach (var kvp in step.SaveAs)
                        {
                            var varName = kvp.Value;
                            object? valueToSet = null;

                            if (keywordResult.Outputs is System.Collections.IDictionary dict)
                            {
                                if (dict.Contains(kvp.Key))
                                    valueToSet = dict[kvp.Key];
                                else
                                    valueToSet = keywordResult.Outputs;
                            }
                            else if (keywordResult.Outputs is not null)
                            {
                                var prop = keywordResult.Outputs.GetType().GetProperty(kvp.Key);
                                if (prop is not null)
                                    valueToSet = prop.GetValue(keywordResult.Outputs);
                                else
                                    valueToSet = keywordResult.Outputs;
                            }
                            else
                            {
                                valueToSet = keywordResult.Outputs;
                            }

                            context.SetVariable(varName, valueToSet);
                        }
                    }

                    break;
                }

                if (attempt < maxAttempts)
                {
                    _logger.LogWarning(
                        "Попытка {Attempt}/{Max} для шага {StepId} не удалась. Повтор через {Delay}ms.",
                        attempt, maxAttempts, step.Id, delayMs);

                    if (delayMs > 0)
                        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    HandleStepFailure(step, stepResult, runResult);
                }
            }
            catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
            {
                stepResult.Status = ExecutionStatus.Failed;
                stepResult.ErrorMessage = $"Таймаут {step.Timeout} превышен.";
                HandleStepFailure(step, stepResult, runResult);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении шага {StepId} (попытка {Attempt}/{Max}).",
                    step.Id, attempt, maxAttempts);

                stepResult.Status = ExecutionStatus.Failed;
                stepResult.ErrorMessage = ex.Message;

                if (attempt < maxAttempts)
                {
                    if (delayMs > 0)
                        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    HandleStepFailure(step, stepResult, runResult);
                }
            }
        }

        stepResult.FinishedAtUtc = DateTimeOffset.UtcNow;
        runResult.Steps.Add(stepResult);
    }

    private void HandleStepFailure(StepNode step, StepExecutionResult stepResult, RunResult runResult)
    {
        if (step.ContinueOnError)
        {
            _logger.LogWarning("Шаг {StepId} failed, но continue_on_error=true.", step.Id);
            stepResult.Status = ExecutionStatus.Passed;
        }
        else
        {
            runResult.Status = ExecutionStatus.Failed;
        }
    }

    private async Task ExecuteIf(
        IfNode ifNode,
        WorkflowDocument document,
        ExecutionContext context,
        RunResult runResult,
        CancellationToken cancellationToken)
    {
        var conditionMet = EvaluateCondition(ifNode.Condition, context);

        _logger.LogInformation(
            "If {IfId}: condition evaluated to {Result}",
            ifNode.Id, conditionMet);

        var nodes = conditionMet ? ifNode.Then : ifNode.Else;

        if (nodes.Count > 0)
        {
            await ExecuteNodes(nodes, document, context, runResult, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteForEach(
        ForEachNode forEach,
        WorkflowDocument document,
        ExecutionContext context,
        RunResult runResult,
        CancellationToken cancellationToken)
    {
        object? items;

        // Если Items — строка, считаем что это выражение
        if (forEach.Items is string itemsExpression)
        {
            var expression = itemsExpression.StartsWith("${")
                ? itemsExpression
                : "${" + itemsExpression + "}";
            items = VariableResolver.ResolveObject(expression, context);
        }
        else
        {
            // Иначе используем как есть (массив, список и т.д.)
            items = forEach.Items;
        }

        if (items is not System.Collections.IEnumerable enumerable)
        {
            _logger.LogWarning("for_each: items не является коллекцией.");
            return;
        }

        var index = 0;
        foreach (var item in enumerable)
        {
            cancellationToken.ThrowIfCancellationRequested();

            context.SetVariable(forEach.As, item);
            context.SetVariable($"{forEach.As}_index", index++);

            await ExecuteNodes(forEach.Steps, document, context, runResult, cancellationToken).ConfigureAwait(false);

            if (runResult.Status == ExecutionStatus.Failed)
                break;
        }
    }

    private async Task ExecuteCall(
        CallNode call,
        WorkflowDocument document,
        ExecutionContext context,
        RunResult runResult,
        CancellationToken cancellationToken)
    {
        if (!document.Tasks.TryGetValue(call.Task, out var calledTask))
        {
            runResult.Status = ExecutionStatus.Failed;

            runResult.Steps.Add(new StepExecutionResult
            {
                StepId = call.Id,
                KeywordName = $"call:{call.Task}",
                Status = ExecutionStatus.Failed,
                ErrorMessage = $"Задача '{call.Task}' не найдена.",
                StartedAtUtc = DateTimeOffset.UtcNow,
                FinishedAtUtc = DateTimeOffset.UtcNow
            });

            return;
        }

        var childContext = new ExecutionContext(
            context.Services,
            new Dictionary<string, object?>(context.Variables));

        foreach (var (key, value) in call.Inputs)
        {
            var resolved = VariableResolver.ResolveObject(value, context);
            childContext.SetVariable(key, resolved);
        }

        await ExecuteNodes(calledTask.Steps, document, childContext, runResult, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(call.SaveAs))
        {
            var outputs = calledTask.Outputs.Keys
                .ToDictionary(k => k, k => childContext.GetVariable(k));

            context.SetVariable(call.SaveAs!, outputs);
        }
    }

    private async Task ExecuteGroup(
        GroupNode group,
        WorkflowDocument document,
        ExecutionContext context,
        RunResult runResult,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Выполняется группа: {GroupName}", group.Name);

        await ExecuteNodes(group.Steps, document, context, runResult, cancellationToken).ConfigureAwait(false);
    }

    private bool ShouldExecute(ConditionNode? condition, IExecutionContext context)
    {
        if (condition is null)
            return true;

        return EvaluateCondition(condition, context);
    }

    private bool EvaluateCondition(ConditionNode condition, IExecutionContext context)
    {
        var leftValue = condition.Var is not null
            ? context.GetVariable(condition.Var)
            : VariableResolver.ResolveObject(condition.Left, context);

        var rightValue = condition.Value ?? VariableResolver.ResolveObject(condition.Right, context);

        return condition.Op.ToLowerInvariant() switch
        {
            "eq" => Equals(leftValue, rightValue),
            "ne" => !Equals(leftValue, rightValue),
            "gt" => CompareNumbers(leftValue, rightValue) > 0,
            "ge" => CompareNumbers(leftValue, rightValue) >= 0,
            "lt" => CompareNumbers(leftValue, rightValue) < 0,
            "le" => CompareNumbers(leftValue, rightValue) <= 0,
            "exists" => leftValue is not null,
            "not_exists" => leftValue is null,
            "contains" => leftValue?.ToString()?.Contains(rightValue?.ToString() ?? "") ?? false,
            "starts_with" => leftValue?.ToString()?.StartsWith(rightValue?.ToString() ?? "") ?? false,
            "ends_with" => leftValue?.ToString()?.EndsWith(rightValue?.ToString() ?? "") ?? false,
            _ => throw new NotSupportedException($"Оператор условия '{condition.Op}' не поддерживается.")
        };
    }

    private static int CompareNumbers(object? left, object? right)
    {
        var leftNum = Convert.ToDouble(left);
        var rightNum = Convert.ToDouble(right);
        return leftNum.CompareTo(rightNum);
    }

    private static int? ParseTimeout(string? timeout)
    {
        if (string.IsNullOrWhiteSpace(timeout))
            return null;

        if (timeout.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
            return int.Parse(timeout[..^2]);

        if (timeout.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            return int.Parse(timeout[..^1]) * 1000;

        if (timeout.EndsWith("m", StringComparison.OrdinalIgnoreCase))
            return int.Parse(timeout[..^1]) * 60 * 1000;

        return int.Parse(timeout);
    }

    private static int ParseDelay(string? delay)
    {
        if (string.IsNullOrWhiteSpace(delay))
            return 0;

        if (delay.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
            return int.Parse(delay[..^2]);

        if (delay.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            return int.Parse(delay[..^1]) * 1000;

        return int.Parse(delay);
    }
}
