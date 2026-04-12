// =============================================================================
// ExecutionRecord.cs — модель записи о выполнении workflow для хранения в БД.
//
// Содержит полную информацию о запуске: статус, время, шаги, ошибки.
// =============================================================================

using System;

namespace AutoFlow.Database;

/// <summary>
/// Запись о выполнении workflow в базе данных.
/// </summary>
public sealed class ExecutionRecord
{
    /// <summary>Уникальный идентификатор записи (auto-increment).</summary>
    public long Id { get; set; }

    /// <summary>Уникальный идентификатор запуска workflow.</summary>
    public required string RunId { get; set; }

    /// <summary>Имя workflow.</summary>
    public required string WorkflowName { get; set; }

    /// <summary>Путь к файлу workflow (если загружен из файла).</summary>
    public string? FilePath { get; set; }

    /// <summary>Статус выполнения: Passed, Failed, Skipped, Cancelled, TimedOut.</summary>
    public required string Status { get; set; }

    /// <summary>Время начала выполнения (UTC) в ISO 8601 формате.</summary>
    public string StartedAtUtc { get; set; } = string.Empty;

    /// <summary>Время окончания выполнения (UTC) в ISO 8601 формате.</summary>
    public string FinishedAtUtc { get; set; } = string.Empty;

    /// <summary>Длительность выполнения в миллисекундах.</summary>
    public long DurationMs { get; set; }

    /// <summary>Количество шагов.</summary>
    public int StepsTotal { get; set; }

    /// <summary>Количество успешных шагов.</summary>
    public int StepsPassed { get; set; }

    /// <summary>Количество failed шагов.</summary>
    public int StepsFailed { get; set; }

    /// <summary>Сообщение об ошибке (если есть).</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>JSON с результатами шагов.</summary>
    public string? StepsJson { get; set; }

    /// <summary>JSON с метаданными запуска.</summary>
    public string? MetadataJson { get; set; }
}
