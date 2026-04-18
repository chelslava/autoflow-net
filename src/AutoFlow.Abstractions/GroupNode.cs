using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Logical grouping of steps. Groups are purely organizational and do not affect execution.
/// Useful for organizing complex workflows into named sections.
/// </summary>
public sealed class GroupNode : IWorkflowNode
{
    /// <summary>
    /// Unique identifier for this group.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable name for the group.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Steps contained within this group.
    /// </summary>
    public List<IWorkflowNode> Steps { get; init; } = new();
}
