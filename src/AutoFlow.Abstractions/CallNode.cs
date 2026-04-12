// Этот код нужен для вызова другой задачи с входными параметрами.
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

public sealed class CallNode : IWorkflowNode
{
    public required string Id { get; init; }

    public required string Task { get; init; }

    public Dictionary<string, object?> Inputs { get; init; } = new();

    public string? SaveAs { get; init; }
}
