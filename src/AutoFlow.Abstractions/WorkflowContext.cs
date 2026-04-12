// =============================================================================
// WorkflowContext.cs — контекст выполнения workflow для lifecycle hooks.
//
// Содержит информацию о запущенном workflow: имя, переменные, метаданные.
// Используется в IWorkflowLifecycleHook для уведомлений о событиях.
// =============================================================================

using System;
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Контекст выполнения workflow. Передаётся в lifecycle hooks.
/// </summary>
public sealed class WorkflowContext
{
    /// <summary>Уникальный идентификатор запуска workflow.</summary>
    public required string RunId { get; init; }

    /// <summary>Имя workflow.</summary>
    public required string WorkflowName { get; init; }

    /// <summary>Путь к файлу workflow (если загружен из файла).</summary>
    public string? FilePath { get; init; }

    /// <summary>Переменные workflow (только для чтения).</summary>
    public IReadOnlyDictionary<string, object?> Variables { get; init; } = new Dictionary<string, object?>();

    /// <summary>Время начала выполнения.</summary>
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Дополнительные метаданные (теги, environment и т.д.).</summary>
    public Dictionary<string, object?> Metadata { get; init; } = new();

    /// <summary>ExecutionContext для доступа к runtime state.</summary>
    public IExecutionContext? ExecutionContext { get; init; }
}
