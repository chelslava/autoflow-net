using System;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Abstractions;

/// <summary>
/// Context passed to keyword handlers during execution.
/// </summary>
public sealed class KeywordContext
{
    /// <summary>
    /// The execution context for accessing runtime state.
    /// </summary>
    public required IExecutionContext ExecutionContext { get; init; }

    /// <summary>
    /// The ID of the current step.
    /// </summary>
    public required string StepId { get; init; }

    /// <summary>
    /// The name of the keyword being executed.
    /// </summary>
    public required string KeywordName { get; init; }

    /// <summary>
    /// Logger for writing log messages.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    /// When the keyword execution started.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
