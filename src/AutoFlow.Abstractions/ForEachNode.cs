using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Loop node that iterates over a collection and executes steps for each item.
/// The current item is accessible via the variable name specified in As.
/// </summary>
public sealed class ForEachNode : IWorkflowNode
{
    /// <summary>
    /// Unique identifier for this for_each block.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Collection to iterate over. Can be an array or variable reference.
    /// </summary>
    public object? Items { get; init; }

    /// <summary>
    /// Variable name for the current item in each iteration.
    /// Example: "item" creates ${item} variable.
    /// </summary>
    public required string As { get; init; }

    /// <summary>
    /// Steps to execute for each item in the collection.
    /// </summary>
    public List<IWorkflowNode> Steps { get; init; } = new();
}
