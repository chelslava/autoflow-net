// Этот код нужен для передачи параметров запуска runtime.
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

public sealed class RuntimeLaunchOptions
{
    public Dictionary<string, object?> Variables { get; init; } = new();

    public bool Verbose { get; init; }
}
