// Этот код нужен для возврата результата работы keyword.
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

public sealed class KeywordResult
{
    public static KeywordResult Success(object? outputs = null, IReadOnlyList<string>? logs = null) =>
        new()
        {
            IsSuccess = true,
            Outputs = outputs,
            Logs = logs ?? []
        };

    public static KeywordResult Failure(string errorMessage, IReadOnlyList<string>? logs = null) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Logs = logs ?? []
        };

    public bool IsSuccess { get; init; }

    public object? Outputs { get; init; }

    public string? ErrorMessage { get; init; }

    public IReadOnlyList<string> Logs { get; init; } = [];
}
