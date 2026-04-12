// Этот код нужен для описания входных аргументов task.
namespace AutoFlow.Abstractions;

public sealed class InputDefinitionNode
{
    public required string Type { get; init; }

    public bool Required { get; init; }

    public bool Secret { get; init; }
}
