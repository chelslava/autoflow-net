// =============================================================================
// LoggingHook.cs — пример lifecycle hook для логирования событий workflow.
//
// Логирует все события: start/end шагов и workflow, ошибки.
// Регистрируется через DI: services.AddSingleton<IWorkflowLifecycleHook, LoggingHook>()
// =============================================================================

using System;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Runtime.Hooks;

/// <summary>
/// Lifecycle hook для логирования всех событий workflow.
/// </summary>
public sealed class LoggingHook : IWorkflowLifecycleHook
{
    private readonly ILogger<LoggingHook> _logger;

    public LoggingHook(ILogger<LoggingHook> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public int Order => 0; // Выполняется первым

    public Task OnWorkflowStartAsync(WorkflowContext ctx)
    {
        _logger.LogInformation(
            "🚀 Workflow started: {WorkflowName} (RunId: {RunId})",
            ctx.WorkflowName, ctx.RunId);
        return Task.CompletedTask;
    }

    public Task OnWorkflowEndAsync(WorkflowContext ctx, RunResult result)
    {
        var icon = result.Status == ExecutionStatus.Passed ? "✅" : "❌";
        _logger.LogInformation(
            "{Icon} Workflow completed: {WorkflowName} - {Status} ({Duration}ms)",
            icon, ctx.WorkflowName, result.Status, (long)result.Duration.TotalMilliseconds);
        return Task.CompletedTask;
    }

    public Task OnStepStartAsync(StepContext ctx)
    {
        _logger.LogDebug(
            "  ▶ Step started: {StepId} ({KeywordName}) - Attempt {Attempt}/{Max}",
            ctx.StepId, ctx.KeywordName, ctx.Attempt, ctx.MaxAttempts);
        return Task.CompletedTask;
    }

    public Task OnStepEndAsync(StepContext ctx, StepExecutionResult result)
    {
        var icon = result.Status == ExecutionStatus.Passed ? "✓" : "✗";
        _logger.LogDebug(
            "  {Icon} Step completed: {StepId} - {Status} ({Duration}ms)",
            icon, ctx.StepId, result.Status, (long)result.Duration.TotalMilliseconds);
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(WorkflowContext ctx, Exception ex)
    {
        _logger.LogError(
            ex,
            "❌ Workflow error: {WorkflowName} (RunId: {RunId})",
            ctx.WorkflowName, ctx.RunId);
        return Task.CompletedTask;
    }
}
