// Этот код нужен для описания retry-политики шага.
namespace AutoFlow.Abstractions;

public sealed class RetryNode
{
    public int Attempts { get; init; } = 1;

    public string? Delay { get; init; }
}
