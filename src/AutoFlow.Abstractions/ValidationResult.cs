using System.Collections.Generic;
using System.Linq;

namespace AutoFlow.Abstractions;

public sealed class ValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public List<ValidationError> Errors { get; } = new();

    public void AddError(string code, string message, string? location = null, string? suggestion = null)
    {
        Errors.Add(new ValidationError(code, message, location, suggestion));
    }

    public void AddErrors(IEnumerable<ValidationError> errors)
    {
        Errors.AddRange(errors);
    }

    public override string ToString()
    {
        return IsValid
            ? "Validation passed"
            : $"Validation failed with {Errors.Count} error(s):\n" +
               string.Join("\n", Errors.Select((e, i) => $"  {i + 1}. {e}"));
    }
}
