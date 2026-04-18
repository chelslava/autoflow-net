using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Node for invoking a reusable task with input parameters.
/// Allows task definitions to be called from multiple places in the workflow.
/// </summary>
public sealed class CallNode : IWorkflowNode
{
    /// <summary>
    /// Unique identifier for this call invocation.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Name of the task to invoke (must be defined in workflow tasks section).
    /// </summary>
    public required string Task { get; init; }

    /// <summary>
    /// Input parameters passed to the called task.
    /// Accessed via ${inputs.param_name} inside the task.
    /// </summary>
    public Dictionary<string, object?> Inputs { get; init; } = new();

    /// <summary>
    /// Variable name to store the called task's outputs.
    /// Example: "result" creates ${result} with task outputs.
    /// </summary>
    public string? SaveAs { get; init; }
}
