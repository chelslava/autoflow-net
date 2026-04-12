// Этот код нужен для полноценного YAML → AST parser-а с поддержкой всех узлов DSL.
using System;
using System.Collections.Generic;
using AutoFlow.Abstractions;
using YamlDotNet.RepresentationModel;

namespace AutoFlow.Parser;

public sealed class YamlWorkflowParser : IWorkflowParser
{
    public WorkflowDocument Parse(string yamlContent)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
            throw new ArgumentException("YAML-документ пустой.", nameof(yamlContent));

        var yaml = new YamlStream();
        using var reader = new System.IO.StringReader(yamlContent);
        yaml.Load(reader);

        if (yaml.Documents.Count == 0)
            throw new ArgumentException("YAML-документ не содержит данных.", nameof(yamlContent));

        var root = yaml.Documents[0].RootNode;
        if (root is not YamlMappingNode rootMapping)
            throw new ArgumentException("YAML-документ должен быть объектом (mapping).", nameof(yamlContent));

        return ParseWorkflowDocument(rootMapping);
    }

    private WorkflowDocument ParseWorkflowDocument(YamlMappingNode root)
    {
        var document = new WorkflowDocument
        {
            SchemaVersion = GetIntValue(root, "schema_version", 1),
            Name = GetRequiredStringValue(root, "name"),
            Imports = ParseImports(root),
            Variables = ParseVariables(root),
            Tasks = ParseTasks(root)
        };

        return document;
    }

    private List<string> ParseImports(YamlMappingNode root)
    {
        if (!root.Children.TryGetValue("imports", out var importsNode))
            return new List<string>();

        if (importsNode is YamlSequenceNode importsSequence)
        {
            var result = new List<string>();
            foreach (var item in importsSequence.Children)
            {
                if (item is YamlScalarNode scalar && !string.IsNullOrWhiteSpace(scalar.Value))
                    result.Add(scalar.Value);
            }
            return result;
        }

        return new List<string>();
    }

    private Dictionary<string, object?> ParseVariables(YamlMappingNode root)
    {
        if (!root.Children.TryGetValue("variables", out var variablesNode))
            return new Dictionary<string, object?>();

        if (variablesNode is not YamlMappingNode variablesMapping)
            return new Dictionary<string, object?>();

        var result = new Dictionary<string, object?>();

        foreach (var kvp in variablesMapping.Children)
        {
            var key = GetStringValue(kvp.Key);
            var value = ParseScalarValue(kvp.Value);
            result[key] = value;
        }

        return result;
    }

    private Dictionary<string, TaskNode> ParseTasks(YamlMappingNode root)
    {
        if (!root.Children.TryGetValue("tasks", out var tasksNode))
            throw new ArgumentException("YAML-документ должен содержать секцию 'tasks'.", nameof(root));

        if (tasksNode is not YamlMappingNode tasksMapping)
            throw new ArgumentException("Секция 'tasks' должна быть объектом (mapping).", nameof(root));

        var result = new Dictionary<string, TaskNode>();

        foreach (var kvp in tasksMapping.Children)
        {
            var taskName = GetStringValue(kvp.Key);
            var taskNode = ParseTask(kvp.Value);
            result[taskName] = taskNode;
        }

        return result;
    }

    private TaskNode ParseTask(YamlNode node)
    {
        if (node is not YamlMappingNode taskMapping)
            throw new ArgumentException("Task должна быть объектом (mapping).", nameof(node));

        return new TaskNode
        {
            Description = GetStringValue(taskMapping, "description"),
            Inputs = ParseInputDefinitions(taskMapping),
            Outputs = ParseOutputDefinitions(taskMapping),
            Steps = ParseSteps(taskMapping),
            OnError = ParseOnError(taskMapping),
            Finally = ParseFinally(taskMapping),
            Timeout = GetStringValue(taskMapping, "timeout")
        };
    }

    private OnErrorNode? ParseOnError(YamlMappingNode taskMapping)
    {
        if (!taskMapping.Children.TryGetValue("on_error", out var onErrorNode))
            return null;

        if (onErrorNode is not YamlMappingNode onErrorMapping)
            return null;

        return new OnErrorNode
        {
            Steps = ParseStepsInField(onErrorMapping, "steps")
        };
    }

    private FinallyNode? ParseFinally(YamlMappingNode taskMapping)
    {
        if (!taskMapping.Children.TryGetValue("finally", out var finallyNode))
            return null;

        if (finallyNode is not YamlMappingNode finallyMapping)
            return null;

        return new FinallyNode
        {
            Steps = ParseStepsInField(finallyMapping, "steps")
        };
    }

    private Dictionary<string, InputDefinitionNode> ParseInputDefinitions(YamlMappingNode taskMapping)
    {
        if (!taskMapping.Children.TryGetValue("inputs", out var inputsNode))
            return new Dictionary<string, InputDefinitionNode>();

        if (inputsNode is not YamlMappingNode inputsMapping)
            return new Dictionary<string, InputDefinitionNode>();

        var result = new Dictionary<string, InputDefinitionNode>();

        foreach (var kvp in inputsMapping.Children)
        {
            var inputName = GetStringValue(kvp.Key);
            var inputDef = ParseInputDefinition(kvp.Value);
            result[inputName] = inputDef;
        }

        return result;
    }

    private InputDefinitionNode ParseInputDefinition(YamlNode node)
    {
        if (node is not YamlMappingNode inputMapping)
            throw new ArgumentException("Input definition должна быть объектом (mapping).", nameof(node));

        return new InputDefinitionNode
        {
            Type = GetRequiredStringValue(inputMapping, "type"),
            Required = GetBoolValue(inputMapping, "required", false),
            Secret = GetBoolValue(inputMapping, "secret", false),
            Default = ParseScalarValue(inputMapping, "default"),
            Description = GetStringValue(inputMapping, "description")
        };
    }

    private Dictionary<string, OutputDefinitionNode> ParseOutputDefinitions(YamlMappingNode taskMapping)
    {
        if (!taskMapping.Children.TryGetValue("outputs", out var outputsNode))
            return new Dictionary<string, OutputDefinitionNode>();

        if (outputsNode is not YamlMappingNode outputsMapping)
            return new Dictionary<string, OutputDefinitionNode>();

        var result = new Dictionary<string, OutputDefinitionNode>();

        foreach (var kvp in outputsMapping.Children)
        {
            var outputName = GetStringValue(kvp.Key);
            var outputDef = ParseOutputDefinition(kvp.Value);
            result[outputName] = outputDef;
        }

        return result;
    }

    private OutputDefinitionNode ParseOutputDefinition(YamlNode node)
    {
        if (node is not YamlMappingNode outputMapping)
            throw new ArgumentException("Output definition должна быть объектом (mapping).", nameof(node));

        return new OutputDefinitionNode
        {
            Type = GetRequiredStringValue(outputMapping, "type")
        };
    }

    private List<IWorkflowNode> ParseSteps(YamlMappingNode taskMapping)
    {
        if (!taskMapping.Children.TryGetValue("steps", out var stepsNode))
            return new List<IWorkflowNode>();

        if (stepsNode is not YamlSequenceNode stepsSequence)
            throw new ArgumentException("Секция 'steps' должна быть массивом (sequence).", nameof(taskMapping));

        var result = new List<IWorkflowNode>();

        foreach (var stepNode in stepsSequence.Children)
        {
            var workflowNode = ParseWorkflowNode(stepNode);
            if (workflowNode is not null)
                result.Add(workflowNode);
        }

        return result;
    }

    private IWorkflowNode? ParseWorkflowNode(YamlNode node)
    {
        if (node is not YamlMappingNode stepMapping)
            throw new ArgumentException("Step должна быть объектом (mapping).", nameof(node));

        if (stepMapping.Children.TryGetValue("step", out var stepContent))
            return ParseStepNode(stepContent);

        if (stepMapping.Children.TryGetValue("if", out var ifContent))
            return ParseIfNode(ifContent);

        if (stepMapping.Children.TryGetValue("foreach", out var forEachContent) ||
            stepMapping.Children.TryGetValue("for_each", out forEachContent))
            return ParseForEachNode(forEachContent);

        if (stepMapping.Children.TryGetValue("call", out var callContent))
            return ParseCallNode(callContent);

        if (stepMapping.Children.TryGetValue("group", out var groupContent))
            return ParseGroupNode(groupContent);

        if (stepMapping.Children.TryGetValue("parallel", out var parallelContent))
            return ParseParallelNode(parallelContent);

        throw new ArgumentException($"Неизвестный тип узла в steps. Доступные: step, if, for_each, call, group, parallel.", nameof(node));
    }

    private StepNode ParseStepNode(YamlNode node)
    {
        if (node is not YamlMappingNode stepMapping)
            throw new ArgumentException("Step content должна быть объектом (mapping).", nameof(node));

        return new StepNode
        {
            Id = GetRequiredStringValue(stepMapping, "id"),
            Uses = GetRequiredStringValue(stepMapping, "uses"),
            With = ParseWith(stepMapping),
            SaveAs = ParseSaveAs(stepMapping),
            ContinueOnError = GetBoolValue(stepMapping, "continue_on_error", false),
            Timeout = GetStringValue(stepMapping, "timeout"),
            Retry = ParseRetry(stepMapping),
            When = ParseCondition(stepMapping, "when")
        };
    }

    private ParallelNode ParseParallelNode(YamlNode node)
    {
        if (node is not YamlMappingNode parallelMapping)
            throw new ArgumentException("Parallel content должна быть объектом (mapping).", nameof(node));

        var errorMode = GetStringValue(parallelMapping, "error_mode")?.ToLowerInvariant() switch
        {
            "continue" => ParallelErrorMode.Continue,
            _ => ParallelErrorMode.FailFast
        };

        return new ParallelNode
        {
            Id = GetStringValue(parallelMapping, "id") ?? $"parallel_{Guid.NewGuid():N}",
            MaxConcurrency = GetIntValue(parallelMapping, "max_concurrency", 10),
            Steps = ParseStepsInField(parallelMapping, "steps"),
            ErrorMode = errorMode
        };
    }

    private Dictionary<string, string>? ParseSaveAs(YamlMappingNode stepMapping)
    {
        if (!stepMapping.Children.TryGetValue("save_as", out var saveAsNode))
            return null;

        if (saveAsNode is YamlScalarNode scalar)
        {
            var varName = scalar.Value;
            if (string.IsNullOrWhiteSpace(varName))
                return null;
            return new Dictionary<string, string> { ["result"] = varName };
        }

        if (saveAsNode is YamlMappingNode mapping)
        {
            var result = new Dictionary<string, string>();
            foreach (var kvp in mapping.Children)
            {
                var key = GetStringValue(kvp.Key);
                var value = GetStringValue(kvp.Value);
                if (!string.IsNullOrWhiteSpace(value))
                    result[key] = value;
            }
            return result.Count > 0 ? result : null;
        }

        return null;
    }

    private IfNode ParseIfNode(YamlNode node)
    {
        if (node is not YamlMappingNode ifMapping)
            throw new ArgumentException("If content должна быть объектом (mapping).", nameof(node));

        return new IfNode
        {
            Id = GetStringValue(ifMapping, "id") ?? $"if_{Guid.NewGuid():N}",
            Condition = ParseRequiredCondition(ifMapping, "condition"),
            Then = ParseStepsInField(ifMapping, "then"),
            Else = ParseStepsInField(ifMapping, "else")
        };
    }

    private ForEachNode ParseForEachNode(YamlNode node)
    {
        if (node is not YamlMappingNode forEachMapping)
            throw new ArgumentException("For_each content должна быть объектом (mapping).", nameof(node));

        object? items;
        if (forEachMapping.Children.TryGetValue("items", out var itemsNode))
        {
            items = ParseScalarValue(itemsNode);
        }
        else
        {
            throw new ArgumentException("Поле 'items' обязательно для foreach.", nameof(node));
        }

        return new ForEachNode
        {
            Id = GetStringValue(forEachMapping, "id") ?? $"foreach_{Guid.NewGuid():N}",
            Items = items,
            As = GetRequiredStringValue(forEachMapping, "as"),
            Steps = ParseStepsInField(forEachMapping, "steps")
        };
    }

    private CallNode ParseCallNode(YamlNode node)
    {
        if (node is not YamlMappingNode callMapping)
            throw new ArgumentException("Call content должна быть объектом (mapping).", nameof(node));

        return new CallNode
        {
            Id = GetStringValue(callMapping, "id") ?? $"call_{Guid.NewGuid():N}",
            Task = GetRequiredStringValue(callMapping, "task"),
            Inputs = ParseCallInputs(callMapping),
            SaveAs = GetStringValue(callMapping, "save_as")
        };
    }

    private Dictionary<string, object?> ParseCallInputs(YamlMappingNode mapping)
    {
        // Поддерживаем оба формата: 'with' и 'inputs'
        if (mapping.Children.TryGetValue("with", out var withNode))
            return ParseInputsFromNode(withNode);

        if (mapping.Children.TryGetValue("inputs", out var inputsNode))
            return ParseInputsFromNode(inputsNode);

        return new Dictionary<string, object?>();
    }

    private Dictionary<string, object?> ParseInputsFromNode(YamlNode node)
    {
        if (node is not YamlMappingNode inputsMapping)
            return new Dictionary<string, object?>();

        var result = new Dictionary<string, object?>();

        foreach (var kvp in inputsMapping.Children)
        {
            var key = GetStringValue(kvp.Key);
            var value = ParseScalarValue(kvp.Value);
            result[key] = value;
        }

        return result;
    }

    private Dictionary<string, object?> ParseWith(YamlMappingNode stepMapping)
    {
        if (!stepMapping.Children.TryGetValue("with", out var withNode))
            return new Dictionary<string, object?>();

        if (withNode is not YamlMappingNode withMapping)
            return new Dictionary<string, object?>();

        var result = new Dictionary<string, object?>();

        foreach (var kvp in withMapping.Children)
        {
            var key = GetStringValue(kvp.Key);
            var value = ParseScalarValue(kvp.Value);
            result[key] = value;
        }

        return result;
    }

    private RetryNode? ParseRetry(YamlMappingNode stepMapping)
    {
        if (!stepMapping.Children.TryGetValue("retry", out var retryNode))
            return null;

        if (retryNode is not YamlMappingNode retryMapping)
            return null;

        var retryType = GetStringValue(retryMapping, "type")?.ToLowerInvariant() switch
        {
            "exponential" => RetryType.Exponential,
            "jitter" => RetryType.Jitter,
            _ => RetryType.Fixed
        };

        return new RetryNode
        {
            Attempts = GetIntValue(retryMapping, "attempts", 1),
            Delay = GetStringValue(retryMapping, "delay"),
            Type = retryType,
            BackoffMultiplier = GetDoubleValue(retryMapping, "backoff_multiplier", 2.0),
            MaxDelay = GetStringValue(retryMapping, "max_delay"),
            RetryOn = ParseStringList(retryMapping, "retry_on"),
            SkipOn = ParseStringList(retryMapping, "skip_on")
        };
    }

    private List<string> ParseStringList(YamlMappingNode mapping, string key)
    {
        if (!mapping.Children.TryGetValue(key, out var node))
            return new List<string>();

        if (node is YamlSequenceNode sequence)
        {
            return sequence.Children
                .OfType<YamlScalarNode>()
                .Select(s => s.Value ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        return new List<string>();
    }

    private GroupNode ParseGroupNode(YamlNode node)
    {
        if (node is not YamlMappingNode groupMapping)
            throw new ArgumentException("Group content должна быть объектом (mapping).", nameof(node));

        return new GroupNode
        {
            Id = GetStringValue(groupMapping, "id") ?? $"group_{Guid.NewGuid():N}",
            Name = GetRequiredStringValue(groupMapping, "name"),
            Steps = ParseStepsInField(groupMapping, "steps")
        };
    }

    private ConditionNode? ParseCondition(YamlMappingNode mapping, string fieldName)
    {
        if (!mapping.Children.TryGetValue(fieldName, out var conditionNode))
            return null;

        return ParseConditionNode(conditionNode);
    }

    private ConditionNode ParseRequiredCondition(YamlMappingNode mapping, string fieldName)
    {
        if (!mapping.Children.TryGetValue(fieldName, out var conditionNode))
            throw new ArgumentException($"Поле '{fieldName}' обязательно для условия.", nameof(mapping));

        return ParseConditionNode(conditionNode);
    }

    private ConditionNode ParseConditionNode(YamlNode node)
    {
        if (node is not YamlMappingNode conditionMapping)
            throw new ArgumentException("Condition должна быть объектом (mapping).", nameof(node));

        // Формат 1: op в явном виде
        if (conditionMapping.Children.ContainsKey("op"))
        {
            return new ConditionNode
            {
                Var = GetStringValue(conditionMapping, "var"),
                Left = GetStringValue(conditionMapping, "left"),
                Op = GetRequiredStringValue(conditionMapping, "op"),
                Value = ParseScalarValue(conditionMapping, "value"),
                Right = ParseScalarValue(conditionMapping, "right")
            };
        }

        // Формат 2: eq: [left, right] и другие операторы
        var operators = new[] { "eq", "ne", "gt", "lt", "ge", "le", "contains", "starts_with", "ends_with", "exists" };
        foreach (var op in operators)
        {
            if (conditionMapping.Children.TryGetValue(op, out var operandNode))
            {
                var operands = operandNode as YamlSequenceNode;
                if (operands is null || operands.Children.Count < 2)
                    throw new ArgumentException($"Оператор '{op}' требует массив из 2 элементов [left, right].", nameof(node));

                return new ConditionNode
                {
                    Op = op,
                    Left = GetStringValue(operands.Children[0]),
                    Right = ParseScalarValue(operands.Children[1])
                };
            }
        }

        throw new ArgumentException($"Condition должна содержать поле 'op' или один из операторов: {string.Join(", ", operators)}.", nameof(node));
    }

    private List<IWorkflowNode> ParseStepsInField(YamlMappingNode mapping, string fieldName)
    {
        if (!mapping.Children.TryGetValue(fieldName, out var stepsNode))
            return new List<IWorkflowNode>();

        if (stepsNode is not YamlSequenceNode stepsSequence)
            throw new ArgumentException($"Поле '{fieldName}' должно быть массивом (sequence).", nameof(mapping));

        var result = new List<IWorkflowNode>();

        foreach (var stepNode in stepsSequence.Children)
        {
            var workflowNode = ParseWorkflowNode(stepNode);
            if (workflowNode is not null)
                result.Add(workflowNode);
        }

        return result;
    }

    private object? ParseScalarValue(YamlMappingNode mapping, string key)
    {
        if (!mapping.Children.TryGetValue(key, out var node))
            return null;

        return ParseScalarValue(node);
    }

    private object? ParseScalarValue(YamlNode node)
    {
        return node switch
        {
            YamlScalarNode scalar => ParseScalarValue(scalar),
            YamlMappingNode mapping => ParseMappingToObject(mapping),
            YamlSequenceNode sequence => ParseSequenceToArray(sequence),
            _ => null
        };
    }

    private object? ParseScalarValue(YamlScalarNode scalar)
    {
        var value = scalar.Value;

        if (string.IsNullOrEmpty(value))
            return null;

        if (bool.TryParse(value, out var boolValue))
            return boolValue;

        if (int.TryParse(value, out var intValue))
            return intValue;

        if (double.TryParse(value, out var doubleValue))
            return doubleValue;

        return value;
    }

    private Dictionary<string, object?> ParseMappingToObject(YamlMappingNode mapping)
    {
        var result = new Dictionary<string, object?>();

        foreach (var kvp in mapping.Children)
        {
            var key = GetStringValue(kvp.Key);
            var value = ParseScalarValue(kvp.Value);
            result[key] = value;
        }

        return result;
    }

    private List<object?> ParseSequenceToArray(YamlSequenceNode sequence)
    {
        var result = new List<object?>();

        foreach (var item in sequence.Children)
        {
            var value = ParseScalarValue(item);
            result.Add(value);
        }

        return result;
    }

    private static string GetStringValue(YamlNode node)
    {
        if (node is YamlScalarNode scalar)
            return scalar.Value ?? string.Empty;

        throw new ArgumentException($"Ожидалась строка, получен {node.NodeType}.", nameof(node));
    }

    private static string? GetStringValue(YamlMappingNode mapping, string key)
    {
        if (!mapping.Children.TryGetValue(key, out var node))
            return null;

        if (node is YamlScalarNode scalar)
            return scalar.Value;

        return null;
    }

    private static string GetRequiredStringValue(YamlMappingNode mapping, string key)
    {
        if (!mapping.Children.TryGetValue(key, out var node))
            throw new ArgumentException($"Обязательное поле '{key}' не найдено.", nameof(mapping));

        if (node is YamlScalarNode scalar)
            return scalar.Value ?? throw new ArgumentException($"Значение поля '{key}' не может быть null.", nameof(mapping));

        throw new ArgumentException($"Поле '{key}' должно быть строкой.", nameof(mapping));
    }

    private static int GetIntValue(YamlMappingNode mapping, string key, int defaultValue)
    {
        if (!mapping.Children.TryGetValue(key, out var node))
            return defaultValue;

        if (node is YamlScalarNode scalar && int.TryParse(scalar.Value, out var result))
            return result;

        return defaultValue;
    }

    private static bool GetBoolValue(YamlMappingNode mapping, string key, bool defaultValue)
    {
        if (!mapping.Children.TryGetValue(key, out var node))
            return defaultValue;

        if (node is YamlScalarNode scalar && bool.TryParse(scalar.Value, out var result))
            return result;

        return defaultValue;
    }

    private static double GetDoubleValue(YamlMappingNode mapping, string key, double defaultValue)
    {
        if (!mapping.Children.TryGetValue(key, out var node))
            return defaultValue;

        if (node is YamlScalarNode scalar && double.TryParse(scalar.Value, out var result))
            return result;

        return defaultValue;
    }
}
