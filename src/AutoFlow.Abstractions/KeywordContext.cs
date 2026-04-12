// Этот код нужен для передачи контекста выполнения в конкретный keyword.
using System;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Abstractions;

public sealed class KeywordContext
{
    public required IExecutionContext ExecutionContext { get; init; }

    public required string StepId { get; init; }

    public required string KeywordName { get; init; }

    public required ILogger Logger { get; init; }

    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
