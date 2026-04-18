namespace AutoFlow.Abstractions;

/// <summary>
/// Definition of a task output parameter.
/// </summary>
public sealed class OutputDefinitionNode
{
    /// <summary>
    /// Type of the output value (string, int, bool, object, etc.).
    /// </summary>
    public required string Type { get; init; }
}
