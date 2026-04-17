// =============================================================================
// WorkflowHookRunner.cs — запуск всех зарегистрированных lifecycle hooks.
//
// Собирает все IWorkflowLifecycleHook из DI и выполняет их в порядке Order.
// Обрабатывает ошибки в hooks, чтобы не ломать выполнение workflow.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Runtime;

/// <summary>
/// Запускает все зарегистрированные lifecycle hooks в порядке Order.
/// Обрабатывает ошибки в hooks, логируя их без прерывания workflow.
/// </summary>
public sealed class WorkflowHookRunner
{
    private readonly IEnumerable<IWorkflowLifecycleHook> _hooks;
    private readonly ILogger<WorkflowHookRunner> _logger;

    public WorkflowHookRunner(
        IEnumerable<IWorkflowLifecycleHook> hooks,
        ILogger<WorkflowHookRunner> logger)
    {
        _hooks = hooks?.OrderBy(h => h.Order) ?? Enumerable.Empty<IWorkflowLifecycleHook>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Вызывает OnWorkflowStartAsync для всех hooks.</summary>
    public async Task OnWorkflowStartAsync(WorkflowContext ctx)
    {
        foreach (var hook in _hooks)
        {
            try
            {
                await hook.OnWorkflowStartAsync(ctx).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not null)
            {
                _logger.LogError(ex, "Ошибка в hook {HookType}.OnWorkflowStartAsync", hook.GetType().Name);
            }
        }
    }

    /// <summary>Вызывает OnWorkflowEndAsync для всех hooks.</summary>
    public async Task OnWorkflowEndAsync(WorkflowContext ctx, RunResult result)
    {
        foreach (var hook in _hooks)
        {
            try
            {
                await hook.OnWorkflowEndAsync(ctx, result).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not null)
            {
                _logger.LogError(ex, "Ошибка в hook {HookType}.OnWorkflowEndAsync", hook.GetType().Name);
            }
        }
    }

    /// <summary>Вызывает OnAfterWorkflowEndAsync для всех hooks.</summary>
    public async Task OnAfterWorkflowEndAsync(WorkflowContext ctx, RunResult result)
    {
        foreach (var hook in _hooks)
        {
            try
            {
                await hook.OnAfterWorkflowEndAsync(ctx, result).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not null)
            {
                _logger.LogError(ex, "Ошибка в hook {HookType}.OnAfterWorkflowEndAsync", hook.GetType().Name);
            }
        }
    }

    /// <summary>Вызывает OnStepStartAsync для всех hooks.</summary>
    public async Task OnStepStartAsync(StepContext ctx)
    {
        foreach (var hook in _hooks)
        {
            try
            {
                await hook.OnStepStartAsync(ctx).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not null)
            {
                _logger.LogError(ex, "Ошибка в hook {HookType}.OnStepStartAsync", hook.GetType().Name);
            }
        }
    }

    /// <summary>Вызывает OnStepEndAsync для всех hooks.</summary>
    public async Task OnStepEndAsync(StepContext ctx, StepExecutionResult result)
    {
        foreach (var hook in _hooks)
        {
            try
            {
                await hook.OnStepEndAsync(ctx, result).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not null)
            {
                _logger.LogError(ex, "Ошибка в hook {HookType}.OnStepEndAsync", hook.GetType().Name);
            }
        }
    }

    /// <summary>Вызывает OnErrorAsync для всех hooks.</summary>
    public async Task OnErrorAsync(WorkflowContext ctx, Exception ex)
    {
        foreach (var hook in _hooks)
        {
            try
            {
                await hook.OnErrorAsync(ctx, ex).ConfigureAwait(false);
            }
            catch (Exception hookEx) when (hookEx is not null)
            {
                _logger.LogError(hookEx, "Ошибка в hook {HookType}.OnErrorAsync", hook.GetType().Name);
            }
        }
    }
}
