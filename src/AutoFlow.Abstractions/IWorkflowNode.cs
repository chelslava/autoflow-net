namespace AutoFlow.Abstractions;

/// <summary>
/// Base interface for all workflow AST nodes.
/// Every node must have a unique identifier for reference and debugging.
/// </summary>
public interface IWorkflowNode
{
    /// <summary>
    /// Unique identifier for this node within the workflow.
    /// </summary>
    string Id { get; }
}
