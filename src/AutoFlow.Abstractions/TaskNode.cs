// Этот код нужен для описания задачи верхнего уровня внутри workflow.
using System.Collections.Generic;

namespace AutoFlow.Abstractions;

public sealed class TaskNode
{
    public string? Description { get; init; }

    public Dictionary<string, InputDefinitionNode> Inputs { get; init; } = new();

    public Dictionary<string, OutputDefinitionNode> Outputs { get; init; } = new();

    public List<IWorkflowNode> Steps { get; init; } = new();
}
