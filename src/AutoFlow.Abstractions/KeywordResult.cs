using System.Collections.Generic;

namespace AutoFlow.Abstractions;

/// <summary>
/// Result of a keyword execution.
/// </summary>
public sealed class KeywordResult
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="outputs">Optional outputs from the keyword.</param>
    /// <param name="logs">Optional log entries.</param>
    /// <returns>A successful KeywordResult.</returns>
    public static KeywordResult Success(object? outputs = null, IReadOnlyList<string>? logs = null) =>
        new()
        {
            IsSuccess = true,
            Outputs = outputs,
            Logs = logs ?? []
        };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="logs">Optional log entries.</param>
    /// <returns>A failed KeywordResult.</returns>
    public static KeywordResult Failure(string errorMessage, IReadOnlyList<string>? logs = null) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Logs = logs ?? []
        };

    /// <summary>
    /// Whether the keyword execution was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Outputs from the keyword execution.
    /// </summary>
    public object? Outputs { get; init; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Log entries from the keyword execution.
    /// </summary>
    public IReadOnlyList<string> Logs { get; init; } = [];
}
