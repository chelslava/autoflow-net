// =============================================================================
// IExecutionRepository.cs — интерфейс репозитория для хранения результатов выполнения.
//
// Предоставляет методы для сохранения, поиска и удаления записей о запусках.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;

namespace AutoFlow.Database;

/// <summary>
/// Репозиторий для хранения результатов выполнения workflows.
/// </summary>
public interface IExecutionRepository
{
    /// <summary>
    /// Сохраняет результат выполнения workflow.
    /// </summary>
    /// <param name="result">Результат выполнения.</param>
    /// <param name="workflowContext">Контекст workflow.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>ID сохранённой записи.</returns>
    Task<long> SaveAsync(
        RunResult result,
        WorkflowContext? workflowContext = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает запись о выполнении по RunId.
    /// </summary>
    Task<ExecutionRecord?> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает запись о выполнении по ID.
    /// </summary>
    Task<ExecutionRecord?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает список выполнений с пагинацией.
    /// </summary>
    /// <param name="workflowName">Фильтр по имени workflow (опционально).</param>
    /// <param name="status">Фильтр по статусу (опционально).</param>
    /// <param name="limit">Максимальное количество записей.</param>
    /// <param name="offset">Смещение для пагинации.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<IReadOnlyList<ExecutionRecord>> GetListAsync(
        string? workflowName = null,
        string? status = null,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет записи старше указанного количества дней.
    /// </summary>
    /// <param name="olderThanDays">Количество дней.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Количество удалённых записей.</returns>
    Task<int> DeleteOlderThanAsync(int olderThanDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет запись по RunId.
    /// </summary>
    Task<bool> DeleteByRunIdAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает статистику по выполнениям.
    /// </summary>
    Task<ExecutionStatistics> GetStatisticsAsync(
        string? workflowName = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default);
}
