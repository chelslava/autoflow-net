using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Xunit;

namespace AutoFlow.Abstractions.Tests;

public sealed class ParallelNodeTests
{
    [Fact]
    public void Constructor_DefaultValues_PropertiesSetCorrectly()
    {
        var node = new ParallelNode
        {
            Id = "test_parallel",
            MaxConcurrency = 3,
            Steps = new List<IWorkflowNode>
            {
                new StepNode { Id = "step1" },
                new StepNode { Id = "step2" }
            }
        };

        Assert.Equal("test_parallel", node.Id);
        Assert.Equal(3, node.MaxConcurrency);
        Assert.Equal(2, node.Steps.Count);
    }

    [Fact]
    public void Constructor_NullSteps_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ParallelNode
            {
                Steps = null!
            });
    }

    [Fact]
    public void Constructor_EmptySteps_ListSetCorrectly()
    {
        var node = new ParallelNode
        {
            Steps = new List<IWorkflowNode>()
        };

        Assert.Empty(node.Steps);
    }

    [Fact]
    public void MaxConcurrency_DefaultValue_Is5()
    {
        var node = new ParallelNode();
        
        Assert.Equal(5, node.MaxConcurrency);
    }

    [Fact]
    public void MaxConcurrency_ValidValue_PropertiesSetCorrectly()
    {
        var node = new ParallelNode
        {
            MaxConcurrency = 10
        };

        Assert.Equal(10, node.MaxConcurrency);
    }

    [Fact]
    public void Id_DefaultValue_IsEmptyString()
    {
        var node = new ParallelNode();

        Assert.Equal(string.Empty, node.Id);
    }

    [Fact]
    public void Steps_CanAddNodes_AddsSuccessfully()
    {
        var node = new ParallelNode();
        var step = new StepNode { Id = "step1" };

        node.Steps.Add(step);

        Assert.Single(node.Steps);
        Assert.Same(step, node.Steps[0]);
    }

    [Fact]
    public void Steps_DuplicateIds_AllowsThem()
    {
        var node = new ParallelNode();

        node.Steps.Add(new StepNode { Id = "step1" });
        node.Steps.Add(new StepNode { Id = "step1" });

        Assert.Equal(2, node.Steps.Count);
    }
}

public sealed class OnErrorNodeTests
{
    [Fact]
    public void Constructor_DefaultValues_PropertiesSetCorrectly()
    {
        var node = new OnErrorNode
        {
            Id = "test_error_handler",
            Steps = new List<IWorkflowNode>
            {
                new StepNode { Id = "error_step1" }
            }
        };

        Assert.Equal("test_error_handler", node.Id);
        Assert.Single(node.Steps);
    }

    [Fact]
    public void Constructor_NullSteps_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OnErrorNode
            {
                Steps = null!
            });
    }

    [Fact]
    public void Constructor_EmptySteps_ListSetCorrectly()
    {
        var node = new OnErrorNode
        {
            Steps = new List<IWorkflowNode>()
        };

        Assert.Empty(node.Steps);
    }

    [Fact]
    public void Id_DefaultValue_IsEmptyString()
    {
        var node = new OnErrorNode();

        Assert.Equal(string.Empty, node.Id);
    }

    [Fact]
    public void Steps_CanAddNodes_AddsSuccessfully()
    {
        var node = new OnErrorNode();
        var step = new StepNode { Id = "error_handler" };

        node.Steps.Add(step);

        Assert.Single(node.Steps);
        Assert.Same(step, node.Steps[0]);
    }

    [Fact]
    public void Steps_MultipleSteps_AddsAll()
    {
        var node = new OnErrorNode();

        node.Steps.Add(new StepNode { Id = "step1" });
        node.Steps.Add(new StepNode { Id = "step2" });
        node.Steps.Add(new StepNode { Id = "step3" });

        Assert.Equal(3, node.Steps.Count);
    }

    [Fact]
    public void Steps_TypeIsList()
    {
        var node = new OnErrorNode();

        Assert.IsType<List<IWorkflowNode>>(node.Steps);
    }
}

public sealed class NodeFactoryTests
{
    [Fact]
    public void CreateParallelNode_DefaultParameters_CreatesNode()
    {
        var node = new ParallelNode();

        Assert.IsType<ParallelNode>(node);
    }

    [Fact]
    public void CreateOnErrorNode_DefaultParameters_CreatesNode()
    {
        var node = new OnErrorNode();

        Assert.IsType<OnErrorNode>(node);
    }

    [Fact]
    public void CreateParallelNode_WithSteps_PropertiesSet()
    {
        var steps = new List<IWorkflowNode>
        {
            new StepNode { Id = "step1" }
        };
        var node = new ParallelNode { Steps = steps };

        Assert.Equal(steps, node.Steps);
    }

    [Fact]
    public void CreateOnErrorNode_WithSteps_PropertiesSet()
    {
        var steps = new List<IWorkflowNode>
        {
            new StepNode { Id = "handler1" }
        };
        var node = new OnErrorNode { Steps = steps };

        Assert.Equal(steps, node.Steps);
    }
}
