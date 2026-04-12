// Этот код нужен для описания статусов выполнения шагов и сценариев.
namespace AutoFlow.Abstractions;

public enum ExecutionStatus
{
    Passed = 0,
    Failed = 1,
    Skipped = 2,
    Cancelled = 3,
    TimedOut = 4
}
