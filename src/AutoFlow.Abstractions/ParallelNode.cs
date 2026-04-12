// =============================================================================
// ParallelNode.cs — узел для параллельного выполнения шагов.
//
// Позволяет выполнять несколько независимых шагов одновременно.
// Поддерживает ограничение max_concurrency для контроля нагрузки.
// =============================================================================

using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Узел для параллельного выполнения шагов.
/// Все шаги выполняются одновременно (с учётом max_concurrency).
/// </summary>
public sealed class ParallelNode : IWorkflowNode
{
    /// <summary>Идентификатор узла.</summary>
    public required string Id { get; init; }

    /// <summary>Максимальное количество одновременно выполняемых шагов.</summary>
    public int MaxConcurrency { get; init; } = 10;

    /// <summary>Шаги для параллельного выполнения.</summary>
    public List<IWorkflowNode> Steps { get; init; } = new();

    /// <summary>Режим обработки ошибок: fail_fast (по умолчанию) или continue.</summary>
    public ParallelErrorMode ErrorMode { get; init; } = ParallelErrorMode.FailFast;
}

/// <summary>
/// Режим обработки ошибок в параллельном блоке.
/// </summary>
public enum ParallelErrorMode
{
    /// <summary>Прервать все шаги при первой ошибке.</summary>
    FailFast,

    /// <summary>Продолжить выполнение всех шагов, собрать ошибки.</summary>
    Continue
}
