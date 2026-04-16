using System;

namespace AutoFlow.Abstractions;

/// <summary>
/// Attribute for marking keyword handler classes with metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class KeywordAttribute : Attribute
{
    /// <summary>
    /// Creates a keyword attribute with the specified name.
    /// </summary>
    /// <param name="name">The keyword name (e.g., "http.request").</param>
    public KeywordAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Keyword name cannot be empty.", nameof(name));

        Name = name;
    }

    /// <summary>
    /// The keyword name used in workflow definitions.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Optional category for grouping keywords.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Optional description of what the keyword does.
    /// </summary>
    public string? Description { get; init; }
}
