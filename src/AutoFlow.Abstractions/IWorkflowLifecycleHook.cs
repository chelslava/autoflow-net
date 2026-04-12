// =============================================================================
// IWorkflowLifecycleHook.cs — интерфейс для перехвата событий lifecycle workflow.
//
// Позволяет подключать кастомную логику на этапах:
// - OnWorkflowStartAsync — перед началом выполнения workflow
// - OnWorkflowEndAsync — после завершения workflow
// - OnStepStartAsync — перед выполнением шага
// - OnStepEndAsync — после выполнения шага
// - OnErrorAsync — при ошибке в workflow
//
// Регистрация через DI: services.AddSingleton<IWorkflowLifecycleHook, MyHook>()
// =============================================================================

using System;
using System.Threading.Tasks;

namespace AutoFlow.Abstractions;

/// <summary>
/// Интерфейс для перехвата событий lifecycle workflow.
/// Реализации регистрируются через DI и вызываются автоматически runtime.
/// </summary>
public interface IWorkflowLifecycleHook
{
    /// <summary>
    /// Вызывается перед началом выполнения workflow.
    /// </summary>
    /// <param name="ctx">Контекст workflow.</param>
    /// <returns>Task для асинхронного выполнения.</returns>
    Task OnWorkflowStartAsync(WorkflowContext ctx) => Task.CompletedTask;

    /// <summary>
    /// Вызывается после завершения workflow (успешно или с ошибкой).
    /// </summary>
    /// <param name="ctx">Контекст workflow.</param>
    /// <param name="result">Результат выполнения workflow.</param>
    /// <returns>Task для асинхронного выполнения.</returns>
    Task OnWorkflowEndAsync(WorkflowContext ctx, RunResult result) => Task.CompletedTask;

    /// <summary>
    /// Вызывается перед выполнением шага.
    /// </summary>
    /// <param name="ctx">Контекст шага.</param>
    /// <returns>Task для асинхронного выполнения.</returns>
    Task OnStepStartAsync(StepContext ctx) => Task.CompletedTask;

    /// <summary>
    /// Вызывается после выполнения шага (успешно или с ошибкой).
    /// </summary>
    /// <param name="ctx">Контекст шага.</param>
    /// <param name="result">Результат выполнения шага.</param>
    /// <returns>Task для асинхронного выполнения.</returns>
    Task OnStepEndAsync(StepContext ctx, StepExecutionResult result) => Task.CompletedTask;

    /// <summary>
    /// Вызывается при необработанной ошибке в workflow.
    /// </summary>
    /// <param name="ctx">Контекст workflow.</param>
    /// <param name="ex">Исключение, вызвавшее ошибку.</param>
    /// <returns>Task для асинхронного выполнения.</returns>
    Task OnErrorAsync(WorkflowContext ctx, Exception ex) => Task.CompletedTask;

    /// <summary>
    /// Порядок выполнения hook (меньше = раньше). По умолчанию 100.
    /// </summary>
    int Order => 100;
}
