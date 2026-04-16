using System;
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Final result of a workflow execution.
/// </summary>
public sealed class RunResult
{
    /// <summary>
    /// Schema version for the result format.
    /// </summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>
    /// Name of the executed workflow.
    /// </summary>
    public required string WorkflowName { get; init; }

    /// <summary>
    /// Final execution status.
    /// </summary>
    public ExecutionStatus Status { get; set; }

    /// <summary>
    /// When execution started.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; init; }

    /// <summary>
    /// When execution finished.
    /// </summary>
    public DateTimeOffset FinishedAtUtc { get; set; }

    /// <summary>
    /// Results of all executed steps.
    /// </summary>
    public List<StepExecutionResult> Steps { get; } = [];

    /// <summary>
    /// Total execution duration.
    /// </summary>
    public TimeSpan Duration => FinishedAtUtc - StartedAtUtc;
}
