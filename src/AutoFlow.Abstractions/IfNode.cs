using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Conditional execution node. Executes different branches based on condition evaluation.
/// </summary>
public sealed class IfNode : IWorkflowNode
{
    /// <summary>
    /// Unique identifier for this if block.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Condition to evaluate. Supports comparison operators.
    /// </summary>
    public required ConditionNode Condition { get; init; }

    /// <summary>
    /// Steps to execute when condition is true.
    /// </summary>
    public List<IWorkflowNode> Then { get; init; } = new();

    /// <summary>
    /// Steps to execute when condition is false.
    /// </summary>
    public List<IWorkflowNode> Else { get; init; } = new();
}
