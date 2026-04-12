// =============================================================================
// TaskNode.cs — описание задачи верхнего уровня внутри workflow.
//
// Содержит steps, inputs, outputs, а также блоки on_error и finally
// для обработки ошибок и очистки ресурсов.
// =============================================================================

using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Задача верхнего уровня внутри workflow. Содержит steps и обработчики ошибок.
/// </summary>
public sealed class TaskNode
{
    /// <summary>Описание задачи.</summary>
    public string? Description { get; init; }

    /// <summary>Входные параметры задачи.</summary>
    public Dictionary<string, InputDefinitionNode> Inputs { get; init; } = new();

    /// <summary>Выходные параметры задачи.</summary>
    public Dictionary<string, OutputDefinitionNode> Outputs { get; init; } = new();

    /// <summary>Основные шаги задачи.</summary>
    public List<IWorkflowNode> Steps { get; init; } = new();

    /// <summary>Блок обработки ошибок. Выполняется при любой ошибке в steps.</summary>
    public OnErrorNode? OnError { get; init; }

    /// <summary>Блок finally. Выполняется в любом случае (успех или ошибка).</summary>
    public FinallyNode? Finally { get; init; }

    /// <summary>Таймаут для всей задачи (например, "5m", "30s").</summary>
    public string? Timeout { get; init; }
}
