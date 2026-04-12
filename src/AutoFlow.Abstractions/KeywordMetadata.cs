// Этот код нужен для хранения metadata о keyword.
namespace AutoFlow.Abstractions;

public sealed record KeywordMetadata(
    string Name,
    string HandlerTypeName,
    string? Category,
    string? Description);
