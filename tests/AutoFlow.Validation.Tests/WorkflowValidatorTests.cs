using System.Collections.Generic;
using AutoFlow.Abstractions;
using AutoFlow.Validation;
using Xunit;

namespace AutoFlow.Validation.Tests;

public sealed class WorkflowValidatorTests
{
    private static WorkflowValidator CreateValidator(params string[] keywordNames)
    {
        var keywords = new List<KeywordMetadata>();
        foreach (var name in keywordNames)
        {
            keywords.Add(new KeywordMetadata(name, $"Handlers.{name}Handler", null, null));
        }

        var provider = new MockKeywordMetadataProvider(keywords);
        return new WorkflowValidator(provider);
    }

    [Fact]
    public void Validate_ValidWorkflow_ReturnsValidResult()
    {
        var validator = CreateValidator("log.info");
        var document = new WorkflowDocument
        {
            Name = "test_flow",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>
            {
                ["main"] = new TaskNode
                {
                    Steps = new List<IWorkflowNode>
                    {
                        new StepNode { Id = "step1", Uses = "log.info" }
                    }
                }
            }
        };

        var result = validator.Validate(document);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        var validator = CreateValidator();
        var document = new WorkflowDocument
        {
            Name = "",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>
            {
                ["main"] = new TaskNode()
            }
        };

        var result = validator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "AF001");
    }

    [Fact]
    public void Validate_NoTasks_ReturnsError()
    {
        var validator = CreateValidator();
        var document = new WorkflowDocument
        {
            Name = "test",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>()
        };

        var result = validator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "AF003");
    }

    [Fact]
    public void Validate_UnknownKeyword_ReturnsError()
    {
        var validator = CreateValidator("log.info");
        var document = new WorkflowDocument
        {
            Name = "test",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>
            {
                ["main"] = new TaskNode
                {
                    Steps = new List<IWorkflowNode>
                    {
                        new StepNode { Id = "step1", Uses = "unknown.keyword" }
                    }
                }
            }
        };

        var result = validator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "AF021");
    }

    [Fact]
    public void Validate_StepWithoutUses_ReturnsError()
    {
        var validator = CreateValidator();
        var document = new WorkflowDocument
        {
            Name = "test",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>
            {
                ["main"] = new TaskNode
                {
                    Steps = new List<IWorkflowNode>
                    {
                        new StepNode { Id = "step1", Uses = "" }
                    }
                }
            }
        };

        var result = validator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "AF020");
    }

    [Fact]
    public void Validate_DuplicateStepIds_ReturnsError()
    {
        var validator = CreateValidator("log.info");
        var document = new WorkflowDocument
        {
            Name = "test",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>
            {
                ["main"] = new TaskNode
                {
                    Steps = new List<IWorkflowNode>
                    {
                        new StepNode { Id = "step1", Uses = "log.info" },
                        new StepNode { Id = "step1", Uses = "log.info" }
                    }
                }
            }
        };

        var result = validator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "AF011");
    }

    [Fact]
    public void Validate_InvalidRetryAttempts_ReturnsError()
    {
        var validator = CreateValidator("log.info");
        var document = new WorkflowDocument
        {
            Name = "test",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>
            {
                ["main"] = new TaskNode
                {
                    Steps = new List<IWorkflowNode>
                    {
                        new StepNode
                        {
                            Id = "step1",
                            Uses = "log.info",
                            Retry = new RetryNode { Attempts = 0 }
                        }
                    }
                }
            }
        };

        var result = validator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "AF022");
    }

    [Fact]
    public void Validate_UnknownTaskInCall_ReturnsError()
    {
        var validator = CreateValidator();
        var document = new WorkflowDocument
        {
            Name = "test",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>
            {
                ["main"] = new TaskNode
                {
                    Steps = new List<IWorkflowNode>
                    {
                        new CallNode { Id = "call1", Task = "nonexistent" }
                    }
                }
            }
        };

        var result = validator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "AF051");
    }

    [Fact]
    public void Validate_ValidCall_ReturnsNoErrors()
    {
        var validator = CreateValidator();
        var document = new WorkflowDocument
        {
            Name = "test",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>
            {
                ["main"] = new TaskNode
                {
                    Steps = new List<IWorkflowNode>
                    {
                        new CallNode { Id = "call1", Task = "helper" }
                    }
                },
                ["helper"] = new TaskNode()
            }
        };

        var result = validator.Validate(document);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_InvalidConditionOperator_ReturnsError()
    {
        var validator = CreateValidator("log.info");
        var document = new WorkflowDocument
        {
            Name = "test",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>
            {
                ["main"] = new TaskNode
                {
                    Steps = new List<IWorkflowNode>
                    {
                        new IfNode
                        {
                            Id = "if1",
                            Condition = new ConditionNode { Op = "invalid_op" },
                            Then = new List<IWorkflowNode>
                            {
                                new StepNode { Id = "step1", Uses = "log.info" }
                            }
                        }
                    }
                }
            }
        };

        var result = validator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "AF031");
    }

    [Fact]
    public void Validate_ForEachWithoutItems_ReturnsError()
    {
        var validator = CreateValidator("log.info");
        var document = new WorkflowDocument
        {
            Name = "test",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>
            {
                ["main"] = new TaskNode
                {
                    Steps = new List<IWorkflowNode>
                    {
                        new ForEachNode
                        {
                            Id = "loop1",
                            ItemsExpression = "",
                            As = "item",
                            Steps = new List<IWorkflowNode>
                            {
                                new StepNode { Id = "step1", Uses = "log.info" }
                            }
                        }
                    }
                }
            }
        };

        var result = validator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "AF040");
    }

    [Fact]
    public void Validate_ForEachWithoutAs_ReturnsError()
    {
        var validator = CreateValidator("log.info");
        var document = new WorkflowDocument
        {
            Name = "test",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>
            {
                ["main"] = new TaskNode
                {
                    Steps = new List<IWorkflowNode>
                    {
                        new ForEachNode
                        {
                            Id = "loop1",
                            ItemsExpression = "${items}",
                            As = "",
                            Steps = new List<IWorkflowNode>
                            {
                                new StepNode { Id = "step1", Uses = "log.info" }
                            }
                        }
                    }
                }
            }
        };

        var result = validator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "AF041");
    }

    [Fact]
    public void Validate_GroupWithoutName_ReturnsError()
    {
        var validator = CreateValidator("log.info");
        var document = new WorkflowDocument
        {
            Name = "test",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>
            {
                ["main"] = new TaskNode
                {
                    Steps = new List<IWorkflowNode>
                    {
                        new GroupNode
                        {
                            Id = "group1",
                            Name = "",
                            Steps = new List<IWorkflowNode>
                            {
                                new StepNode { Id = "step1", Uses = "log.info" }
                            }
                        }
                    }
                }
            }
        };

        var result = validator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "AF060");
    }

    [Fact]
    public void Validate_NestedNodes_ValidatesRecursively()
    {
        var validator = CreateValidator("log.info");
        var document = new WorkflowDocument
        {
            Name = "test",
            SchemaVersion = 1,
            Tasks = new Dictionary<string, TaskNode>
            {
                ["main"] = new TaskNode
                {
                    Steps = new List<IWorkflowNode>
                    {
                        new IfNode
                        {
                            Id = "if1",
                            Condition = new ConditionNode { Var = "x", Op = "eq", Value = 1 },
                            Then = new List<IWorkflowNode>
                            {
                                new ForEachNode
                                {
                                    Id = "loop1",
                                    ItemsExpression = "${items}",
                                    As = "item",
                                    Steps = new List<IWorkflowNode>
                                    {
                                        new StepNode { Id = "step1", Uses = "unknown" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var result = validator.Validate(document);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "AF021" && e.Location?.Contains("if1") == true);
    }

    private sealed class MockKeywordMetadataProvider : IKeywordMetadataProvider
    {
        private readonly IReadOnlyCollection<KeywordMetadata> _keywords;

        public MockKeywordMetadataProvider(IReadOnlyCollection<KeywordMetadata> keywords)
        {
            _keywords = keywords;
        }

        public IReadOnlyCollection<KeywordMetadata> GetKeywords() => _keywords;
    }
}
