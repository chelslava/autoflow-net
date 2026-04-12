// Этот код нужен для канонического узла выполнения keyword.
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

public sealed class StepNode : IWorkflowNode
{
    public required string Id { get; init; }

    public required string Uses { get; init; }

    public Dictionary<string, object?> With { get; init; } = new();

    public string? SaveAs { get; init; }

    public bool ContinueOnError { get; init; }

    public string? Timeout { get; init; }

    public RetryNode? Retry { get; init; }

    public ConditionNode? When { get; init; }
}
