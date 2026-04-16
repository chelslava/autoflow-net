using System;
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Result of a single step execution.
/// </summary>
public sealed class StepExecutionResult
{
    /// <summary>
    /// The ID of the executed step.
    /// </summary>
    public required string StepId { get; init; }

    /// <summary>
    /// The name of the keyword that was executed.
    /// </summary>
    public required string KeywordName { get; init; }

    /// <summary>
    /// The execution status of the step.
    /// </summary>
    public ExecutionStatus Status { get; set; }

    /// <summary>
    /// When the step execution started.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; init; }

    /// <summary>
    /// When the step execution finished.
    /// </summary>
    public DateTimeOffset FinishedAtUtc { get; set; }

    /// <summary>
    /// Outputs from the keyword execution.
    /// </summary>
    public object? Outputs { get; set; }

    /// <summary>
    /// Error message if the step failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Log entries from the step execution.
    /// </summary>
    public List<string> Logs { get; } = [];

    /// <summary>
    /// Duration of the step execution.
    /// </summary>
    public TimeSpan Duration => FinishedAtUtc - StartedAtUtc;
}
