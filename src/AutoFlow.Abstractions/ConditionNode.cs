// Этот код нужен для структурного описания условий без строкового expression language.
namespace AutoFlow.Abstractions;

public sealed class ConditionNode
{
    public string? Var { get; init; }

    public string? Left { get; init; }

    public required string Op { get; init; }

    public object? Value { get; init; }

    public object? Right { get; init; }
}
