// =============================================================================
// InputDefinitionNode.cs — описание входного аргумента task.
//
// Определяет тип, обязательность, секретность и значение по умолчанию.
// =============================================================================

namespace AutoFlow.Abstractions;

/// <summary>
/// Описание входного аргумента задачи.
/// </summary>
public sealed class InputDefinitionNode
{
    /// <summary>Тип аргумента (string, int, bool, object и т.д.).</summary>
    public required string Type { get; init; }

    /// <summary>Обязателен ли аргумент.</summary>
    public bool Required { get; init; }

    /// <summary>Является ли аргумент секретом (маскируется в логах).</summary>
    public bool Secret { get; init; }

    /// <summary>Значение по умолчанию, если аргумент не передан.</summary>
    public object? Default { get; init; }

    /// <summary>Описание аргумента.</summary>
    public string? Description { get; init; }
}
