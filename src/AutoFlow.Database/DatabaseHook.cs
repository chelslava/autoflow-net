// =============================================================================
// DatabaseHook.cs — lifecycle hook для сохранения результатов в базу данных.
//
// Сохраняет результаты выполнения workflow в SQLite через IExecutionRepository.
// =============================================================================

using System;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Database;

/// <summary>
/// Lifecycle hook для сохранения результатов выполнения в базу данных.
/// </summary>
public sealed class DatabaseHook : IWorkflowLifecycleHook
{
    private readonly IExecutionRepository _repository;
    private readonly ILogger<DatabaseHook> _logger;

    /// <summary>
    /// Создаёт hook с указанным репозиторием.
    /// </summary>
    public DatabaseHook(IExecutionRepository repository, ILogger<DatabaseHook> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => "Database";

    /// <inheritdoc />
    public int Priority => 100;

    /// <inheritdoc />
    public Task OnWorkflowStartAsync(WorkflowContext context)
    {
        _logger.LogDebug("Workflow {WorkflowName} started (RunId: {RunId})", context.WorkflowName, context.RunId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnWorkflowEndAsync(WorkflowContext context, RunResult result)
    {
        _logger.LogDebug(
            "Workflow {WorkflowName} finished with status {Status}",
            context.WorkflowName, result.Status);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task OnAfterWorkflowEndAsync(WorkflowContext context, RunResult result)
    {
        try
        {
            var id = await _repository.SaveAsync(result, context).ConfigureAwait(false);
            _logger.LogInformation(
                "Результат выполнения {WorkflowName} (RunId: {RunId}) сохранён в БД (ID: {Id})",
                context.WorkflowName, context.RunId, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении результата в БД");
        }
    }

    /// <inheritdoc />
    public Task OnErrorAsync(WorkflowContext context, Exception exception)
    {
        _logger.LogError(
            exception,
            "Workflow {WorkflowName} failed (RunId: {RunId})",
            context.WorkflowName, context.RunId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnStepStartAsync(StepContext context)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OnStepEndAsync(StepContext context, StepExecutionResult result)
    {
        return Task.CompletedTask;
    }
}
