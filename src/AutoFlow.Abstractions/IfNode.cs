// Этот код нужен для описания условного выполнения шагов.
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

public sealed class IfNode : IWorkflowNode
{
    public required string Id { get; init; }

    public required ConditionNode Condition { get; init; }

    public List<IWorkflowNode> Then { get; init; } = new();

    public List<IWorkflowNode> Else { get; init; } = new();
}
