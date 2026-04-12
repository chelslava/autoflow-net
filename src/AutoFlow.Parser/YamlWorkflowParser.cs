// Этот код нужен для стартового parser-а YAML-документа в AST.
// В первой версии здесь специально реализован минимальный каркас, который можно безопасно расширять.
using System;
using AutoFlow.Abstractions;

namespace AutoFlow.Parser;

public sealed class YamlWorkflowParser : IWorkflowParser
{
    public WorkflowDocument Parse(string yamlContent)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
            throw new ArgumentException("YAML-документ пустой.", nameof(yamlContent));

        // Временная заглушка.
        // Здесь позже нужно реализовать полноценный YAML -> AST parser.
        return new WorkflowDocument
        {
            Name = "stub-workflow",
            Variables = new(),
            Tasks = new()
            {
                ["main"] = new TaskNode
                {
                    Steps =
                    [
                        new StepNode
                        {
                            Id = "log_start",
                            Uses = "log.info",
                            With = new()
                            {
                                ["message"] = "Стартовый workflow из заглушки parser-а."
                            }
                        }
                    ]
                }
            }
        };
    }
}
