// Этот код нужен для описания цикла по коллекции.
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

public sealed class ForEachNode : IWorkflowNode
{
    public required string Id { get; init; }

    public object? Items { get; init; }

    public required string As { get; init; }

    public List<IWorkflowNode> Steps { get; init; } = new();
}
