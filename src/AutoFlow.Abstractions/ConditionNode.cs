namespace AutoFlow.Abstractions;

/// <summary>
/// Represents a condition for conditional execution (if/when).
/// Supports comparison operators: eq, ne, gt, lt, gte, lte, contains, starts_with, ends_with.
/// </summary>
public sealed class ConditionNode
{
    /// <summary>
    /// Variable reference to compare (e.g., "${status}").
    /// </summary>
    public string? Var { get; init; }

    /// <summary>
    /// Left operand for binary comparison.
    /// </summary>
    public string? Left { get; init; }

    /// <summary>
    /// Comparison operator: eq, ne, gt, lt, gte, lte, contains, starts_with, ends_with, exists.
    /// </summary>
    public required string Op { get; init; }

    /// <summary>
    /// Value to compare against (for Var-based conditions).
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Right operand for binary comparison (when using Left).
    /// </summary>
    public object? Right { get; init; }
}
