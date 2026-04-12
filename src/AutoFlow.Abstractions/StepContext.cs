// =============================================================================
// StepContext.cs — контекст выполнения шага для lifecycle hooks.
//
// Содержит информацию о текущем выполняемом шаге: ID, keyword, аргументы.
// Используется в IWorkflowLifecycleHook для уведомлений о событиях шагов.
// =============================================================================

using System;
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Контекст выполнения шага. Передаётся в lifecycle hooks.
/// </summary>
public sealed class StepContext
{
    /// <summary>Уникальный идентификатор запуска workflow.</summary>
    public required string RunId { get; init; }

    /// <summary>Идентификатор шага в workflow.</summary>
    public required string StepId { get; init; }

    /// <summary>Имя keyword.</summary>
    public required string KeywordName { get; init; }

    /// <summary>Аргументы шага (до разрешения переменных).</summary>
    public IReadOnlyDictionary<string, object?> RawArgs { get; init; } = new Dictionary<string, object?>();

    /// <summary>Аргументы шага (после разрешения переменных).</summary>
    public IReadOnlyDictionary<string, object?> ResolvedArgs { get; init; } = new Dictionary<string, object?>();

    /// <summary>Время начала выполнения шага.</summary>
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Номер попытки выполнения (1-based).</summary>
    public int Attempt { get; init; } = 1;

    /// <summary>Максимальное количество попыток.</summary>
    public int MaxAttempts { get; init; } = 1;

    /// <summary>ExecutionContext для доступа к runtime state.</summary>
    public IExecutionContext? ExecutionContext { get; init; }

    /// <summary>Создаёт копию с обновлёнными ResolvedArgs.</summary>
    public StepContext WithResolvedArgs(IReadOnlyDictionary<string, object?> resolvedArgs)
    {
        return new StepContext
        {
            RunId = RunId,
            StepId = StepId,
            KeywordName = KeywordName,
            RawArgs = RawArgs,
            ResolvedArgs = resolvedArgs,
            StartedAtUtc = StartedAtUtc,
            Attempt = Attempt,
            MaxAttempts = MaxAttempts,
            ExecutionContext = ExecutionContext
        };
    }
}
