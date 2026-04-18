// =============================================================================
// SQLiteExecutionRepository.cs — реализация репозитория на SQLite.
//
// Использует Dapper для маппинга и Microsoft.Data.Sqlite для подключения.
// Автоматически создаёт таблицы при первом запуске.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Database;

/// <summary>
/// Реализация репозитория на SQLite.
/// </summary>
public sealed class SQLiteExecutionRepository : IExecutionRepository, IDisposable
{
    private const int DefaultCommandTimeoutSeconds = 30;
    
    private readonly string _connectionString;
    private readonly ILogger<SQLiteExecutionRepository> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SecretMasker? _secretMasker;
    private readonly int _commandTimeoutSeconds;
    private bool _initialized;

    public SQLiteExecutionRepository(string databasePath, ILogger<SQLiteExecutionRepository> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(logger);

        _connectionString = $"Data Source={databasePath}";
        _logger = logger;
        _commandTimeoutSeconds = DefaultCommandTimeoutSeconds;
    }

    public SQLiteExecutionRepository(
        string databasePath,
        ILogger<SQLiteExecutionRepository> logger,
        SecretMasker? secretMasker = null) : this(databasePath, logger)
    {
        _secretMasker = secretMasker;
    }

    public SQLiteExecutionRepository(
        string databasePath,
        ILogger<SQLiteExecutionRepository> logger,
        SecretMasker? secretMasker,
        int commandTimeoutSeconds) : this(databasePath, logger, secretMasker)
    {
        _commandTimeoutSeconds = commandTimeoutSeconds > 0 
            ? commandTimeoutSeconds 
            : DefaultCommandTimeoutSeconds;
    }

    private SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA busy_timeout = {_commandTimeoutSeconds * 1000};";
        command.ExecuteNonQuery();
        return connection;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-checked locking pattern - second check is intentional for thread safety
#pragma warning disable CA1508 // Avoid dead conditional code
            if (_initialized)
                return;
#pragma warning restore CA1508

            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS executions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    run_id TEXT NOT NULL UNIQUE,
                    workflow_name TEXT NOT NULL,
                    file_path TEXT,
                    status TEXT NOT NULL,
                    started_at_utc TEXT NOT NULL,
                    finished_at_utc TEXT NOT NULL,
                    duration_ms INTEGER NOT NULL,
                    steps_total INTEGER NOT NULL,
                    steps_passed INTEGER NOT NULL,
                    steps_failed INTEGER NOT NULL,
                    error_message TEXT,
                    steps_json TEXT,
                    metadata_json TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_executions_run_id ON executions(run_id);
                CREATE INDEX IF NOT EXISTS idx_executions_workflow_name ON executions(workflow_name);
                CREATE INDEX IF NOT EXISTS idx_executions_status ON executions(status);
                CREATE INDEX IF NOT EXISTS idx_executions_started_at ON executions(started_at_utc);
            ").ConfigureAwait(false);

            _initialized = true;
            _logger.LogInformation("База данных инициализирована");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<long> SaveAsync(
        RunResult result,
        WorkflowContext? workflowContext = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var stepsJson = JsonSerializer.Serialize(result.Steps, JsonOptions.Default);
            var metadataJson = workflowContext is not null
                ? JsonSerializer.Serialize(workflowContext.Metadata, JsonOptions.Default)
                : null;

            if (_secretMasker is not null)
            {
                stepsJson = _secretMasker.Mask(stepsJson);
                metadataJson = metadataJson is not null ? _secretMasker.Mask(metadataJson) : null;
            }

            var sql = @"
            INSERT INTO executions (
                run_id, workflow_name, file_path, status,
                started_at_utc, finished_at_utc, duration_ms,
                steps_total, steps_passed, steps_failed,
                error_message, steps_json, metadata_json
            ) VALUES (
                @RunId, @WorkflowName, @FilePath, @Status,
                @StartedAtUtc, @FinishedAtUtc, @DurationMs,
                @StepsTotal, @StepsPassed, @StepsFailed,
                @ErrorMessage, @StepsJson, @MetadataJson
            );
            SELECT last_insert_rowid();
        ";

            var id = await connection.QuerySingleAsync<long>(sql, new
            {
                RunId = workflowContext?.RunId ?? Guid.NewGuid().ToString("N"),
                result.WorkflowName,
                FilePath = workflowContext?.FilePath,
                Status = result.Status.ToString(),
                StartedAtUtc = result.StartedAtUtc.ToString("O"),
                FinishedAtUtc = result.FinishedAtUtc.ToString("O"),
                DurationMs = (long)result.Duration.TotalMilliseconds,
                StepsTotal = result.Steps.Count,
                StepsPassed = result.Steps.Count(s => s.Status == ExecutionStatus.Passed),
                StepsFailed = result.Steps.Count(s => s.Status == ExecutionStatus.Failed),
                ErrorMessage = result.Steps.FirstOrDefault(s => s.ErrorMessage is not null)?.ErrorMessage,
                StepsJson = stepsJson,
                MetadataJson = metadataJson
            }, transaction).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Сохранено выполнение {RunId} (ID: {Id})", workflowContext?.RunId, id);

            return id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<ExecutionRecord?> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = @"
            SELECT 
                id AS Id,
                run_id AS RunId,
                workflow_name AS WorkflowName,
                file_path AS FilePath,
                status AS Status,
                started_at_utc AS StartedAtUtc,
                finished_at_utc AS FinishedAtUtc,
                duration_ms AS DurationMs,
                steps_total AS StepsTotal,
                steps_passed AS StepsPassed,
                steps_failed AS StepsFailed,
                error_message AS ErrorMessage,
                steps_json AS StepsJson,
                metadata_json AS MetadataJson
            FROM executions 
            WHERE run_id = @RunId";

        return await connection.QueryFirstOrDefaultAsync<ExecutionRecord>(sql, new { RunId = runId })
            .ConfigureAwait(false);
    }

    public async Task<ExecutionRecord?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = @"
            SELECT 
                id AS Id,
                run_id AS RunId,
                workflow_name AS WorkflowName,
                file_path AS FilePath,
                status AS Status,
                started_at_utc AS StartedAtUtc,
                finished_at_utc AS FinishedAtUtc,
                duration_ms AS DurationMs,
                steps_total AS StepsTotal,
                steps_passed AS StepsPassed,
                steps_failed AS StepsFailed,
                error_message AS ErrorMessage,
                steps_json AS StepsJson,
                metadata_json AS MetadataJson
            FROM executions 
            WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<ExecutionRecord>(sql, new { Id = id })
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ExecutionRecord>> GetListAsync(
        string? workflowName = null,
        string? status = null,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = @"
            SELECT 
                id AS Id,
                run_id AS RunId,
                workflow_name AS WorkflowName,
                file_path AS FilePath,
                status AS Status,
                started_at_utc AS StartedAtUtc,
                finished_at_utc AS FinishedAtUtc,
                duration_ms AS DurationMs,
                steps_total AS StepsTotal,
                steps_passed AS StepsPassed,
                steps_failed AS StepsFailed,
                error_message AS ErrorMessage,
                steps_json AS StepsJson,
                metadata_json AS MetadataJson
            FROM executions
            WHERE (@WorkflowName IS NULL OR workflow_name = @WorkflowName)
              AND (@Status IS NULL OR status = @Status)
            ORDER BY started_at_utc DESC
            LIMIT @Limit OFFSET @Offset
        ";

        var results = await connection.QueryAsync<ExecutionRecord>(sql, new
        {
            WorkflowName = workflowName,
            Status = status,
            Limit = limit,
            Offset = offset
        }).ConfigureAwait(false);

        return results.ToList();
    }

    public async Task<int> DeleteOlderThanAsync(int olderThanDays, CancellationToken cancellationToken = default)
    {
        if (olderThanDays < 0)
            throw new ArgumentOutOfRangeException(nameof(olderThanDays), "Количество дней должно быть неотрицательным");

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-olderThanDays).ToString("O");

        var sql = "DELETE FROM executions WHERE started_at_utc < @CutoffDate";
        var deleted = await connection.ExecuteAsync(sql, new { CutoffDate = cutoffDate })
            .ConfigureAwait(false);

        _logger.LogInformation("Удалено {Count} записей старше {Days} дней", deleted, olderThanDays);

        return deleted;
    }

    public async Task<bool> DeleteByRunIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var sql = "DELETE FROM executions WHERE run_id = @RunId";
        var deleted = await connection.ExecuteAsync(sql, new { RunId = runId })
            .ConfigureAwait(false);

        return deleted > 0;
    }

    public async Task<ExecutionStatistics> GetStatisticsAsync(
        string? workflowName = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var sql = @"
            SELECT
                COUNT(*) as TotalRuns,
                SUM(CASE WHEN status = 'Passed' THEN 1 ELSE 0 END) as PassedRuns,
                SUM(CASE WHEN status = 'Failed' THEN 1 ELSE 0 END) as FailedRuns,
                AVG(duration_ms) as AverageDurationMs,
                MIN(duration_ms) as MinDurationMs,
                MAX(duration_ms) as MaxDurationMs,
                SUM(steps_total) as TotalSteps
            FROM executions
            WHERE (@WorkflowName IS NULL OR workflow_name = @WorkflowName)
              AND (@From IS NULL OR started_at_utc >= @From)
              AND (@To IS NULL OR started_at_utc <= @To)
        ";

        var result = await connection.QuerySingleAsync<dynamic>(sql, new
        {
            WorkflowName = workflowName,
            From = from?.ToString("O"),
            To = to?.ToString("O")
        }).ConfigureAwait(false);

        return new ExecutionStatistics
        {
            TotalRuns = Convert.ToInt32(result.TotalRuns ?? 0),
            PassedRuns = Convert.ToInt32(result.PassedRuns ?? 0),
            FailedRuns = Convert.ToInt32(result.FailedRuns ?? 0),
            AverageDurationMs = Convert.ToDouble(result.AverageDurationMs ?? 0),
            MinDurationMs = Convert.ToInt64(result.MinDurationMs ?? 0),
            MaxDurationMs = Convert.ToInt64(result.MaxDurationMs ?? 0),
            TotalSteps = Convert.ToInt32(result.TotalSteps ?? 0),
            From = from,
            To = to
        };
    }

    public void Dispose()
    {
        _initLock.Dispose();
    }
}

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true
    };
}
