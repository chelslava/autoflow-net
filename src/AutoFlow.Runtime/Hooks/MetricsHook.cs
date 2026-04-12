// =============================================================================
// MetricsHook.cs — пример lifecycle hook для сбора метрик.
//
// Собирает метрики выполнения: длительность шагов, количество ошибок, throughput.
// Интегрируется с Prometheus, OpenTelemetry или другим monitoring system.
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AutoFlow.Abstractions;

namespace AutoFlow.Runtime.Hooks;

/// <summary>
/// Lifecycle hook для сбора метрик выполнения workflow.
/// </summary>
public sealed class MetricsHook : IWorkflowLifecycleHook
{
    private readonly ConcurrentDictionary<string, WorkflowMetrics> _metrics = new();

    public int Order => 10;

    public Task OnWorkflowStartAsync(WorkflowContext ctx)
    {
        _metrics[ctx.RunId] = new WorkflowMetrics
        {
            WorkflowName = ctx.WorkflowName,
            StartedAt = ctx.StartedAtUtc
        };
        return Task.CompletedTask;
    }

    public Task OnWorkflowEndAsync(WorkflowContext ctx, RunResult result)
    {
        if (_metrics.TryRemove(ctx.RunId, out var metrics))
        {
            metrics.FinishedAt = result.FinishedAtUtc;
            metrics.TotalSteps = result.Steps.Count;
            metrics.PassedSteps = result.Steps.Count(s => s.Status == ExecutionStatus.Passed);
            metrics.FailedSteps = result.Steps.Count(s => s.Status == ExecutionStatus.Failed);
            metrics.Status = result.Status;

            // Здесь можно отправить метрики в Prometheus/OpenTelemetry
            // Пример: _prometheusExporter.Export(metrics);
        }
        return Task.CompletedTask;
    }

    public Task OnStepEndAsync(StepContext ctx, StepExecutionResult result)
    {
        if (_metrics.TryGetValue(ctx.RunId, out var metrics))
        {
            metrics.StepMetrics.Add(new StepMetric
            {
                StepId = ctx.StepId,
                KeywordName = ctx.KeywordName,
                DurationMs = (long)result.Duration.TotalMilliseconds,
                Status = result.Status,
                Attempt = ctx.Attempt
            });
        }
        return Task.CompletedTask;
    }

    /// <summary>Получить накопленные метрики (для тестирования).</summary>
    public WorkflowMetrics? GetMetrics(string runId)
    {
        return _metrics.TryGetValue(runId, out var metrics) ? metrics : null;
    }
}

/// <summary>Метрики выполнения workflow.</summary>
public sealed class WorkflowMetrics
{
    public string WorkflowName { get; init; } = string.Empty;
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; set; }
    public ExecutionStatus Status { get; set; }
    public int TotalSteps { get; set; }
    public int PassedSteps { get; set; }
    public int FailedSteps { get; set; }
    public List<StepMetric> StepMetrics { get; init; } = new();
}

/// <summary>Метрики выполнения шага.</summary>
public sealed class StepMetric
{
    public string StepId { get; init; } = string.Empty;
    public string KeywordName { get; init; } = string.Empty;
    public long DurationMs { get; init; }
    public ExecutionStatus Status { get; init; }
    public int Attempt { get; init; }
}
