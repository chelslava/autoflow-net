using System;
using System.Collections.Generic;
using System.Linq;
using AutoFlow.Abstractions;

namespace AutoFlow.Validation;

public sealed class WorkflowValidator : IWorkflowValidator
{
    private readonly IKeywordMetadataProvider _keywordProvider;

    public WorkflowValidator(IKeywordMetadataProvider keywordProvider)
    {
        _keywordProvider = keywordProvider ?? throw new ArgumentNullException(nameof(keywordProvider));
    }

    public ValidationResult Validate(WorkflowDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var result = new ValidationResult();

        ValidateDocumentStructure(document, result);
        ValidateTasks(document, result);

        return result;
    }

    private static void ValidateDocumentStructure(WorkflowDocument document, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(document.Name))
        {
            result.AddError(
                "AF001",
                "Workflow name is required",
                "workflow.name",
                "Add 'name:' at the top level of your workflow");
        }

        if (document.SchemaVersion < 1)
        {
            result.AddError(
                "AF002",
                $"Invalid schema_version: {document.SchemaVersion}",
                "workflow.schema_version",
                "Use 'schema_version: 1'");
        }

        if (document.Tasks.Count == 0)
        {
            result.AddError(
                "AF003",
                "Workflow must have at least one task",
                "workflow.tasks",
                "Add a 'tasks:' section with at least one task");
        }
    }

    private void ValidateTasks(WorkflowDocument document, ValidationResult result)
    {
        var taskNames = document.Tasks.Keys.ToHashSet();

        foreach (var (taskName, task) in document.Tasks)
        {
            ValidateTask(taskName, task, taskNames, result);
        }
    }

    private void ValidateTask(string taskName, TaskNode task, HashSet<string> taskNames, ValidationResult result)
    {
        var location = $"tasks.{taskName}";

        ValidateNodes(task.Steps, location, taskNames, result);
    }

    private void ValidateNodes(List<IWorkflowNode> nodes, string location, HashSet<string> taskNames, ValidationResult result)
    {
        var stepIds = new HashSet<string>();

        foreach (var node in nodes)
        {
            ValidateNode(node, location, taskNames, stepIds, result);
        }
    }

    private void ValidateNode(IWorkflowNode node, string parentLocation, HashSet<string> taskNames, HashSet<string> stepIds, ValidationResult result)
    {
        var location = $"{parentLocation}.{node.Id}";

        if (string.IsNullOrWhiteSpace(node.Id))
        {
            result.AddError(
                "AF010",
                "Node must have an 'id' property",
                parentLocation,
                "Add 'id:' to each step, if, for_each, call, or group node");
            return;
        }

        if (stepIds.Contains(node.Id))
        {
            result.AddError(
                "AF011",
                $"Duplicate id '{node.Id}'",
                location,
                "Each node must have a unique id within its task");
        }
        else
        {
            stepIds.Add(node.Id);
        }

        switch (node)
        {
            case StepNode step:
                ValidateStep(step, location, result);
                break;
            case IfNode ifNode:
                ValidateIf(ifNode, location, taskNames, stepIds, result);
                break;
            case ForEachNode forEach:
                ValidateForEach(forEach, location, taskNames, stepIds, result);
                break;
            case CallNode call:
                ValidateCall(call, location, taskNames, result);
                break;
            case GroupNode group:
                ValidateGroup(group, location, taskNames, stepIds, result);
                break;
        }
    }

    private void ValidateStep(StepNode step, string location, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(step.Uses))
        {
            result.AddError(
                "AF020",
                "Step must have 'uses' property",
                location,
                "Specify keyword name, e.g., 'uses: log.info'");
            return;
        }

        var keywords = _keywordProvider.GetKeywords();
        var keywordNames = keywords.Select(k => k.Name).ToHashSet();

        if (!keywordNames.Contains(step.Uses))
        {
            result.AddError(
                "AF021",
                $"Unknown keyword: '{step.Uses}'",
                location,
                $"Available keywords: {string.Join(", ", keywordNames.Take(5))}...");
        }

        if (step.Retry is not null)
        {
            if (step.Retry.Attempts < 1)
            {
                result.AddError(
                    "AF022",
                    $"Retry attempts must be >= 1, got {step.Retry.Attempts}",
                    $"{location}.retry.attempts",
                    "Use 'attempts: 1' or more");
            }
        }
    }

    private void ValidateIf(IfNode ifNode, string location, HashSet<string> taskNames, HashSet<string> stepIds, ValidationResult result)
    {
        ValidateCondition(ifNode.Condition, $"{location}.condition", result);

        if (ifNode.Then.Count > 0)
        {
            ValidateNodes(ifNode.Then, $"{location}.then", taskNames, result);
        }

        if (ifNode.Else.Count > 0)
        {
            ValidateNodes(ifNode.Else, $"{location}.else", taskNames, result);
        }
    }

    private void ValidateCondition(ConditionNode condition, string location, ValidationResult result)
    {
        var validOps = new HashSet<string> { "eq", "ne", "gt", "ge", "lt", "le", "exists", "not_exists", "contains", "starts_with", "ends_with" };

        if (string.IsNullOrWhiteSpace(condition.Op))
        {
            result.AddError(
                "AF030",
                "Condition must have 'op' property",
                location,
                $"Valid operators: {string.Join(", ", validOps)}");
        }
        else if (!validOps.Contains(condition.Op))
        {
            result.AddError(
                "AF031",
                $"Unknown condition operator: '{condition.Op}'",
                location,
                $"Valid operators: {string.Join(", ", validOps)}");
        }
    }

    private void ValidateForEach(ForEachNode forEach, string location, HashSet<string> taskNames, HashSet<string> stepIds, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(forEach.ItemsExpression))
        {
            result.AddError(
                "AF040",
                "for_each must have 'items' property",
                location,
                "Use 'items: ${list}' to specify the collection to iterate");
        }

        if (string.IsNullOrWhiteSpace(forEach.As))
        {
            result.AddError(
                "AF041",
                "for_each must have 'as' property",
                location,
                "Use 'as: item' to name the loop variable");
        }

        if (forEach.Steps.Count == 0)
        {
            result.AddError(
                "AF042",
                "for_each must have at least one step",
                $"{location}.steps",
                "Add steps inside the for_each block");
        }
        else
        {
            ValidateNodes(forEach.Steps, $"{location}.steps", taskNames, result);
        }
    }

    private void ValidateCall(CallNode call, string location, HashSet<string> taskNames, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(call.Task))
        {
            result.AddError(
                "AF050",
                "call must have 'task' property",
                location,
                "Specify the task name to call");
            return;
        }

        if (!taskNames.Contains(call.Task))
        {
            result.AddError(
                "AF051",
                $"Unknown task: '{call.Task}'",
                location,
                $"Available tasks: {string.Join(", ", taskNames)}");
        }
    }

    private void ValidateGroup(GroupNode group, string location, HashSet<string> taskNames, HashSet<string> stepIds, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(group.Name))
        {
            result.AddError(
                "AF060",
                "group must have 'name' property",
                location,
                "Add a descriptive name for the group");
        }

        if (group.Steps.Count == 0)
        {
            result.AddError(
                "AF061",
                "group must have at least one step",
                $"{location}.steps",
                "Add steps inside the group");
        }
        else
        {
            ValidateNodes(group.Steps, $"{location}.steps", taskNames, result);
        }
    }
}
