namespace AutoFlow.Abstractions;

/// <summary>
/// Execution status for steps and workflows.
/// </summary>
public enum ExecutionStatus
{
    /// <summary>
    /// Execution completed successfully.
    /// </summary>
    Passed = 0,

    /// <summary>
    /// Execution failed with an error.
    /// </summary>
    Failed = 1,

    /// <summary>
    /// Execution was skipped.
    /// </summary>
    Skipped = 2,

    /// <summary>
    /// Execution was cancelled.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Execution exceeded its timeout.
    /// </summary>
    TimedOut = 4
}
