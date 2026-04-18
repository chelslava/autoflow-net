// =============================================================================
// RuntimeEngine.cs — движок выполнения workflow с поддержкой hooks, secrets, parallel.
//
// Поддерживает:
// - Lifecycle hooks (OnWorkflowStart, OnStepStart, OnError и т.д.)
// - Secrets management с маскированием
// - Parallel execution
// - DI scope per workflow
// - OnError/Finally blocks
// - Exponential backoff retry
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using AutoFlow.Runtime.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Runtime;

public sealed class RuntimeEngine : IRuntimeEngine
{
    private readonly IServiceProvider _rootServiceProvider;
    private readonly KeywordExecutor _keywordExecutor;
    private readonly WorkflowHookRunner _hookRunner;
    private readonly SecretResolver _secretResolver;
    private readonly TelemetryProvider _telemetry;
    private readonly ILogger<RuntimeEngine> _logger;

    public RuntimeEngine(
        IServiceProvider serviceProvider,
        KeywordExecutor keywordExecutor,
        WorkflowHookRunner hookRunner,
        SecretResolver secretResolver,
        TelemetryProvider telemetry,
        ILogger<RuntimeEngine> logger)
    {
        _rootServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _keywordExecutor = keywordExecutor ?? throw new ArgumentNullException(nameof(keywordExecutor));
        _hookRunner = hookRunner ?? throw new ArgumentNullException(nameof(hookRunner));
        _secretResolver = secretResolver ?? throw new ArgumentNullException(nameof(secretResolver));
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RunResult> ExecuteAsync(
        WorkflowDocument document,
        RuntimeLaunchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);

        var runId = options.RunId ?? Guid.NewGuid().ToString("N");
        var startedAt = DateTimeOffset.UtcNow;

        _telemetry.RecordWorkflowStart(document.Name);
        using var workflowSpan = _telemetry.StartWorkflowSpan(document.Name, runId);

        var runResult = new RunResult
        {
            WorkflowName = document.Name,
            StartedAtUtc = startedAt,
            Status = ExecutionStatus.Passed
        };

        // Создаём DI scope для каждого workflow
        using var scope = _rootServiceProvider.CreateScope();
        var scopedServices = scope.ServiceProvider;

        var variables = document.Variables
            .Concat(options.Variables)
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last().Value, StringComparer.OrdinalIgnoreCase);

        var context = new ExecutionContext(scopedServices, variables);

        var workflowContext = new WorkflowContext
        {
            RunId = runId,
            WorkflowName = document.Name,
            FilePath = document.FilePath,
            Variables = variables,
            StartedAtUtc = startedAt,
            ExecutionContext = context
        };

        try
        {
            using var logScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["RunId"] = runId,
                ["WorkflowName"] = document.Name,
                ["FilePath"] = document.FilePath ?? ""
            });

            _logger.LogInformation(
                "Starting workflow {WorkflowName} (RunId: {RunId})",
                document.Name, runId);

            await _hookRunner.OnWorkflowStartAsync(workflowContext).ConfigureAwait(false);

            if (!document.Tasks.TryGetValue("main", out var mainTask))
                throw new InvalidOperationException("В документе не найдена задача 'main'.");

            // Выполняем main task с поддержкой on_error/finally
            await ExecuteTaskWithHandlers(mainTask, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            runResult.Status = ExecutionStatus.Failed;
            _logger.LogError(ex, "Workflow {WorkflowName} configuration error (RunId: {RunId})",
                document.Name, runId);
            await _hookRunner.OnErrorAsync(workflowContext, ex).ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            runResult.Status = ExecutionStatus.Failed;
            _logger.LogError(ex, "Workflow {WorkflowName} argument error (RunId: {RunId})",
                document.Name, runId);
            await _hookRunner.OnErrorAsync(workflowContext, ex).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            runResult.Status = ExecutionStatus.Failed;
            _logger.LogWarning("Workflow {WorkflowName} cancelled (RunId: {RunId})",
                document.Name, runId);
            await _hookRunner.OnErrorAsync(workflowContext, ex).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            runResult.Status = ExecutionStatus.Failed;
            _logger.LogError(ex, "Workflow {WorkflowName} unexpected error (RunId: {RunId}) - {Duration}ms",
                document.Name, runId, (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds);
            await _hookRunner.OnErrorAsync(workflowContext, ex).ConfigureAwait(false);
        }
        finally
        {
            runResult.FinishedAtUtc = DateTimeOffset.UtcNow;
            
            var durationMs = (runResult.FinishedAtUtc - startedAt).TotalMilliseconds;
            _telemetry.RecordWorkflowEnd(document.Name, runResult.Status.ToString(), durationMs);

            _logger.LogInformation(
                "Workflow {WorkflowName} completed (RunId: {RunId}) - Status: {Status}, Duration: {Duration}ms",
                document.Name, runId, runResult.Status, (long)durationMs);
            
            workflowSpan?.SetStatus(runResult.Status == ExecutionStatus.Passed ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
            workflowSpan?.Dispose();
            
            await _hookRunner.OnWorkflowEndAsync(workflowContext, runResult).ConfigureAwait(false);
            await _hookRunner.OnAfterWorkflowEndAsync(workflowContext, runResult).ConfigureAwait(false);
        }

        return runResult;
    }

    private async Task ExecuteTaskWithHandlers(
        TaskNode task,
        WorkflowDocument document,
        ExecutionContext context,
        RunResult runResult,
        WorkflowContext workflowContext,
        CancellationToken cancellationToken)
    {
        Exception? taskException = null;

        try
        {
            await ExecuteNodes(task.Steps, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Task execution cancelled");
            throw;
        }
        catch (Exception ex)
        {
            taskException = ex;
            runResult.Status = ExecutionStatus.Failed;
            _logger.LogError(ex, "Ошибка при выполнении task");

            // Выполняем on_error блок
            if (task.OnError is not null)
            {
                _logger.LogInformation("Выполняется on_error блок");
                await ExecuteNodes(task.OnError.Steps, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            // Выполняем finally блок в любом случае
            if (task.Finally is not null)
            {
                _logger.LogInformation("Выполняется finally блок");
                try
                {
                    await ExecuteNodes(task.Finally.Steps, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
                }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в finally блоке");
            }
            }
        }

        if (taskException is not null && task.OnError is null)
            throw taskException;
    }

    private async Task ExecuteNodes(
        List<IWorkflowNode> nodes,
        WorkflowDocument document,
        ExecutionContext context,
        RunResult runResult,
        WorkflowContext workflowContext,
        CancellationToken cancellationToken)
    {
        foreach (var node in nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (runResult.Status == ExecutionStatus.Failed)
                break;

            await ExecuteNode(node, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteNode(
        IWorkflowNode node,
        WorkflowDocument document,
        ExecutionContext context,
        RunResult runResult,
        WorkflowContext workflowContext,
        CancellationToken cancellationToken)
    {
        switch (node)
        {
            case StepNode stepNode:
                await ExecuteStep(stepNode, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
                break;

            case IfNode ifNode:
                await ExecuteIf(ifNode, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
                break;

            case ForEachNode forEachNode:
                await ExecuteForEach(forEachNode, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
                break;

            case CallNode callNode:
                await ExecuteCall(callNode, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
                break;

            case GroupNode groupNode:
                await ExecuteGroup(groupNode, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
                break;

            case ParallelNode parallelNode:
                await ExecuteParallel(parallelNode, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
                break;

            default:
                throw new NotSupportedException($"Узел типа '{node.GetType().Name}' не поддерживается.");
        }
    }

    private async Task ExecuteStep(
        StepNode step,
        ExecutionContext context,
        RunResult runResult,
        WorkflowContext workflowContext,
        CancellationToken cancellationToken)
    {
        if (!ShouldExecute(step.When, context))
        {
            _logger.LogInformation("Шаг {StepId} пропущен (condition=false).", step.Id);
            return;
        }

        var stepStartedAt = DateTimeOffset.UtcNow;
        using var stepSpan = _telemetry.StartStepSpan(workflowContext.WorkflowName, step.Id, step.Uses);

        var stepResult = new StepExecutionResult
        {
            StepId = step.Id,
            KeywordName = step.Uses,
            StartedAtUtc = stepStartedAt,
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
        var baseDelayMs = ParseDelay(step.Retry?.Delay);
        var retryType = step.Retry?.Type ?? RetryType.Fixed;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var stepContext = new StepContext
            {
                RunId = workflowContext.RunId,
                StepId = step.Id,
                KeywordName = step.Uses,
                RawArgs = step.With,
                Attempt = attempt,
                MaxAttempts = maxAttempts,
                StartedAtUtc = DateTimeOffset.UtcNow,
                ExecutionContext = context
            };

            try
            {
                await _hookRunner.OnStepStartAsync(stepContext).ConfigureAwait(false);

                var resolvedArgs = VariableResolver.ResolveObject(step.With, context);

                // Разрешаем секреты
                resolvedArgs = await _secretResolver.ResolveObjectAsync(resolvedArgs, cancellationToken).ConfigureAwait(false);
                stepContext = stepContext.WithResolvedArgs(resolvedArgs as Dictionary<string, object?> ?? new Dictionary<string, object?>());

                var keywordResult = await _keywordExecutor.ExecuteAsync(
                    context,
                    step.Id,
                    step.Uses,
                    resolvedArgs,
                    effectiveToken).ConfigureAwait(false);

                // Маскируем секреты в результатах
                if (keywordResult.ErrorMessage is not null)
                    keywordResult = KeywordResult.Failure(
                        _secretResolver.GetMasker().Mask(keywordResult.ErrorMessage),
                        keywordResult.Logs);

                stepResult.Status = keywordResult.IsSuccess
                    ? ExecutionStatus.Passed
                    : ExecutionStatus.Failed;

                stepResult.Outputs = keywordResult.Outputs;
                stepResult.ErrorMessage = keywordResult.ErrorMessage;
                stepResult.Logs.AddRange(keywordResult.Logs.Select(l => _secretResolver.GetMasker().Mask(l)));

                await _hookRunner.OnStepEndAsync(stepContext, stepResult).ConfigureAwait(false);

                if (keywordResult.IsSuccess)
                {
                    context.SetStepResult(step.Id, keywordResult.Outputs);

                    if (step.SaveAs is not null)
                    {
                        foreach (var kvp in step.SaveAs)
                        {
                            var varName = kvp.Value;
                            object? valueToSet;

                            if (keywordResult.Outputs is System.Collections.IDictionary dict)
                            {
                                valueToSet = dict.Contains(kvp.Key) ? dict[kvp.Key] : keywordResult.Outputs;
                            }
                            else if (keywordResult.Outputs is not null)
                            {
                                var prop = keywordResult.Outputs.GetType().GetProperty(kvp.Key);
                                valueToSet = prop is not null ? prop.GetValue(keywordResult.Outputs) : keywordResult.Outputs;
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
                    var delayMs = CalculateRetryDelay(retryType, baseDelayMs, attempt, step.Retry);
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
                await _hookRunner.OnStepEndAsync(stepContext, stepResult).ConfigureAwait(false);
                HandleStepFailure(step, stepResult, runResult);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении шага {StepId} (попытка {Attempt}/{Max}).",
                    step.Id, attempt, maxAttempts);

                stepResult.Status = ExecutionStatus.Failed;
                stepResult.ErrorMessage = _secretResolver.GetMasker().Mask(ex.Message);

                if (attempt < maxAttempts && ShouldRetry(ex, step.Retry))
                {
                    var delayMs = CalculateRetryDelay(retryType, baseDelayMs, attempt, step.Retry);
                    if (delayMs > 0)
                        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
                else if (attempt >= maxAttempts)
                {
                    await _hookRunner.OnStepEndAsync(stepContext, stepResult).ConfigureAwait(false);
                    HandleStepFailure(step, stepResult, runResult);
                }
            }
        }

        stepResult.FinishedAtUtc = DateTimeOffset.UtcNow;
        
        var stepDurationMs = (stepResult.FinishedAtUtc - stepStartedAt).TotalMilliseconds;
        _telemetry.RecordStepEnd(workflowContext.WorkflowName, step.Uses, stepResult.Status.ToString(), stepDurationMs);
        
        stepSpan?.SetStatus(stepResult.Status == ExecutionStatus.Passed ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
        stepSpan?.Dispose();
        
        runResult.AddStep(stepResult);
    }

    private bool ShouldRetry(Exception ex, RetryNode? retry)
    {
        if (retry is null)
            return true;

        // Если указан SkipOn, проверяем что исключение не в списке
        if (retry.SkipOn.Count > 0)
        {
            var exceptionName = ex.GetType().Name;
            if (retry.SkipOn.Any(skip => skip.Equals(exceptionName, StringComparison.OrdinalIgnoreCase) ||
                                          ex.GetType().FullName?.Contains(skip, StringComparison.OrdinalIgnoreCase) == true))
                return false;
        }

        // Если указан RetryOn, проверяем что исключение в списке
        if (retry.RetryOn.Count > 0)
        {
            var exceptionName = ex.GetType().Name;
            return retry.RetryOn.Any(retry => retry.Equals(exceptionName, StringComparison.OrdinalIgnoreCase) ||
                                               ex.GetType().FullName?.Contains(retry, StringComparison.OrdinalIgnoreCase) == true);
        }

        return true;
    }

    private int CalculateRetryDelay(RetryType retryType, int baseDelayMs, int attempt, RetryNode? retry)
    {
        if (baseDelayMs <= 0)
            return 0;

        return retryType switch
        {
            RetryType.Exponential => CalculateExponentialDelay(baseDelayMs, attempt, retry?.BackoffMultiplier ?? 2.0, retry?.MaxDelay),
            RetryType.Jitter => baseDelayMs + Random.Shared.Next(0, baseDelayMs),
            _ => baseDelayMs
        };
    }

    private int CalculateExponentialDelay(int baseDelayMs, int attempt, double multiplier, string? maxDelay)
    {
        var delay = (int)(baseDelayMs * Math.Pow(multiplier, attempt - 1));
        var maxDelayMs = ParseDelay(maxDelay);

        if (maxDelayMs > 0 && delay > maxDelayMs)
            delay = maxDelayMs;

        return delay;
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

    private async Task ExecuteParallel(
        ParallelNode parallel,
        WorkflowDocument document,
        ExecutionContext context,
        RunResult runResult,
        WorkflowContext workflowContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Выполняется параллельный блок: {Id}, max_concurrency={MaxConcurrency}",
            parallel.Id, parallel.MaxConcurrency);

        using var semaphore = new SemaphoreSlim(parallel.MaxConcurrency);
        var exceptions = new ConcurrentBag<Exception>();
        var failed = false;

        var tasks = parallel.Steps.Select(async node =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (parallel.ErrorMode == ParallelErrorMode.FailFast && failed)
                    return;

                await ExecuteNode(node, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                failed = true;
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        await Task.WhenAll(tasks).ConfigureAwait(false);

        if (exceptions.Count > 0)
        {
            if (parallel.ErrorMode == ParallelErrorMode.FailFast)
            {
                throw new AggregateException("Ошибки в параллельном блоке", exceptions);
            }
            else
            {
                _logger.LogWarning("Параллельный блок завершён с {Count} ошибками", exceptions.Count);
            }
        }
    }

    private async Task ExecuteIf(
        IfNode ifNode,
        WorkflowDocument document,
        ExecutionContext context,
        RunResult runResult,
        WorkflowContext workflowContext,
        CancellationToken cancellationToken)
    {
        var conditionMet = EvaluateCondition(ifNode.Condition, context);

        _logger.LogInformation(
            "If {IfId}: condition evaluated to {Result}",
            ifNode.Id, conditionMet);

        var nodes = conditionMet ? ifNode.Then : ifNode.Else;

        if (nodes.Count > 0)
        {
            await ExecuteNodes(nodes, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExecuteForEach(
        ForEachNode forEach,
        WorkflowDocument document,
        ExecutionContext context,
        RunResult runResult,
        WorkflowContext workflowContext,
        CancellationToken cancellationToken)
    {
        object? items;

        if (forEach.Items is string itemsExpression)
        {
            var expression = itemsExpression.StartsWith("${")
                ? itemsExpression
                : "${" + itemsExpression + "}";
            items = VariableResolver.ResolveObject(expression, context);
        }
        else
        {
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

            await ExecuteNodes(forEach.Steps, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);

            if (runResult.Status == ExecutionStatus.Failed)
                break;
        }
    }

    private async Task ExecuteCall(
        CallNode call,
        WorkflowDocument document,
        ExecutionContext context,
        RunResult runResult,
        WorkflowContext workflowContext,
        CancellationToken cancellationToken)
    {
        if (!document.Tasks.TryGetValue(call.Task, out var calledTask))
        {
            runResult.Status = ExecutionStatus.Failed;

            runResult.AddStep(new StepExecutionResult
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

        // Применяем default значения для inputs
        foreach (var (inputName, inputDef) in calledTask.Inputs)
        {
            if (call.Inputs.TryGetValue(inputName, out var inputValue))
            {
                var resolved = VariableResolver.ResolveObject(inputValue, context);
                childContext.SetVariable(inputName, resolved);
            }
            else if (inputDef.Default is not null)
            {
                childContext.SetVariable(inputName, inputDef.Default);
            }
            else if (inputDef.Required)
            {
                throw new ArgumentException($"Обязательный параметр '{inputName}' не передан для задачи '{call.Task}'.");
            }
        }

        await ExecuteTaskWithHandlers(calledTask, document, childContext, runResult, workflowContext, cancellationToken).ConfigureAwait(false);

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
        WorkflowContext workflowContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Выполняется группа: {GroupName}", group.Name);

        await ExecuteNodes(group.Steps, document, context, runResult, workflowContext, cancellationToken).ConfigureAwait(false);
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
