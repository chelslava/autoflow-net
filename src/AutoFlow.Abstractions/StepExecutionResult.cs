// Этот код нужен для хранения результата выполнения одного шага.
using System;
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

public sealed class StepExecutionResult
{
    public required string StepId { get; init; }

    public required string KeywordName { get; init; }

    public ExecutionStatus Status { get; set; }

    public DateTimeOffset StartedAtUtc { get; init; }

    public DateTimeOffset FinishedAtUtc { get; set; }

    public object? Outputs { get; set; }

    public string? ErrorMessage { get; set; }

    public List<string> Logs { get; } = [];

    public TimeSpan Duration => FinishedAtUtc - StartedAtUtc;
}
