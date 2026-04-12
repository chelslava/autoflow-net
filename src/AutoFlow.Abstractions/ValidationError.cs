namespace AutoFlow.Abstractions;

public sealed record ValidationError(
    string Code,
    string Message,
    string? Location = null,
    string? Suggestion = null)
{
    public override string ToString()
    {
        var result = $"[{Code}] {Message}";
        if (Location is not null)
            result += $" (location: {Location})";
        if (Suggestion is not null)
            result += $" — {Suggestion}";
        return result;
    }
}
