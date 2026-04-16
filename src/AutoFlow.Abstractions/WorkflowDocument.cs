using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Canonical AST model for a workflow document.
/// </summary>
public sealed class WorkflowDocument
{
    /// <summary>
    /// Schema version for the workflow format.
    /// </summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>
    /// Name of the workflow.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// File path if loaded from a file.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// List of imported workflow files.
    /// </summary>
    public List<string> Imports { get; init; } = new();

    /// <summary>
    /// Workflow-level variables.
    /// </summary>
    public Dictionary<string, object?> Variables { get; init; } = new();

    /// <summary>
    /// Tasks defined in the workflow.
    /// </summary>
    public Dictionary<string, TaskNode> Tasks { get; init; } = new();
}
