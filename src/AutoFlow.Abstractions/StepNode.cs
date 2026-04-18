namespace AutoFlow.Abstractions;

/// <summary>
/// Represents a single executable step in a workflow.
/// A step invokes a keyword with specified arguments and optional configuration.
/// </summary>
public sealed class StepNode : IWorkflowNode
{
    /// <summary>
    /// Unique identifier for this step within the workflow.
    /// Used to reference step outputs via ${steps.id.outputs.*}.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The keyword to execute (e.g., "http.request", "files.read", "browser.open").
    /// </summary>
    public required string Uses { get; init; }

    /// <summary>
    /// Input arguments passed to the keyword handler.
    /// Keys depend on the specific keyword being used.
    /// </summary>
    public Dictionary<string, object?> With { get; init; } = new();

    /// <summary>
    /// Output variable mappings. Maps keyword outputs to variable names.
    /// Example: { "body" = "response_data" } creates ${response_data}.
    /// </summary>
    public Dictionary<string, string>? SaveAs { get; init; }

    /// <summary>
    /// If true, workflow continues even if this step fails.
    /// </summary>
    public bool ContinueOnError { get; init; }

    /// <summary>
    /// Maximum execution time (e.g., "30s", "5m").
    /// </summary>
    public string? Timeout { get; init; }

    /// <summary>
    /// Retry policy for transient failures.
    /// </summary>
    public RetryNode? Retry { get; init; }

    /// <summary>
    /// Conditional execution. Step runs only when condition evaluates to true.
    /// </summary>
    public ConditionNode? When { get; init; }
}
