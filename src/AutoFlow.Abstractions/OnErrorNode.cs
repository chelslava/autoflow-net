// =============================================================================
// OnErrorNode.cs — описание блока обработки ошибок на уровне task.
//
// Содержит шаги для выполнения при ошибке в основном блоке steps.
// Выполняется даже при continue_on_error на шагах.
// =============================================================================

using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Описание блока обработки ошибок на уровне task.
/// </summary>
public sealed class OnErrorNode
{
    /// <summary>Шаги для выполнения при ошибке.</summary>
    public List<IWorkflowNode> Steps { get; init; } = new();
}

/// <summary>
/// Описание блока finally на уровне task.
/// </summary>
public sealed class FinallyNode
{
    /// <summary>Шаги для выполнения в любом случае (успех или ошибка).</summary>
    public List<IWorkflowNode> Steps { get; init; } = new();
}
