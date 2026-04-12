// Этот код нужен для логической группировки шагов.
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

public sealed class GroupNode : IWorkflowNode
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public List<IWorkflowNode> Steps { get; init; } = new();
}
