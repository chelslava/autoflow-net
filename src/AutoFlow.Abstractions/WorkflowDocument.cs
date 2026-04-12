// Этот код нужен для канонической AST-модели workflow-документа.
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

public sealed class WorkflowDocument
{
    public int SchemaVersion { get; init; } = 1;

    public required string Name { get; init; }

    public string? FilePath { get; init; }

    public List<string> Imports { get; init; } = new();

    public Dictionary<string, object?> Variables { get; init; } = new();

    public Dictionary<string, TaskNode> Tasks { get; init; } = new();
}
