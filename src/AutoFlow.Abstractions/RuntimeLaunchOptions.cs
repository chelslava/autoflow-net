// =============================================================================
// RuntimeLaunchOptions.cs — параметры запуска workflow runtime.
//
// Содержит переменные, RunId, флаги verbose и другие опции запуска.
// =============================================================================

using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Параметры запуска workflow.
/// </summary>
public sealed class RuntimeLaunchOptions
{
    /// <summary>Переменные для передачи в workflow.</summary>
    public Dictionary<string, object?> Variables { get; init; } = new();

    /// <summary>Уникальный идентификатор запуска. Если не указан, генерируется автоматически.</summary>
    public string? RunId { get; init; }

    /// <summary>Включить детальное логирование.</summary>
    public bool Verbose { get; init; }

    /// <summary>Включить checkpoint для pause/resume.</summary>
    public bool EnableCheckpoints { get; init; }

    /// <summary>Возобновить с указанного checkpoint.</summary>
    public string? ResumeFromCheckpoint { get; init; }
}
