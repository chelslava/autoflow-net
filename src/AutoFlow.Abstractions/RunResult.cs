// Этот код нужен для хранения итогового результата выполнения workflow.
using System;
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

public sealed class RunResult
{
    public string SchemaVersion { get; init; } = "1.0";

    public required string WorkflowName { get; init; }

    public ExecutionStatus Status { get; set; }

    public DateTimeOffset StartedAtUtc { get; init; }

    public DateTimeOffset FinishedAtUtc { get; set; }

    public List<StepExecutionResult> Steps { get; } = [];

    public TimeSpan Duration => FinishedAtUtc - StartedAtUtc;
}
