using AutoFlow.Abstractions;
using AutoFlow.Parser;
using AutoFlow.Reporting;
using AutoFlow.Runtime;
using AutoFlow.Runtime.Hooks;
using AutoFlow.Runtime.Resilience;
using AutoFlow.Runtime.Secrets;
using AutoFlow.Runtime.Telemetry;
using AutoFlow.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Cli.Tests;

public sealed class CliDependencyInjectionTests
{
    [Fact]
    public void ConfigureServices_RegistersAllRequiredServices()
    {
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Logging.ClearProviders();

        var registry = new KeywordRegistry();

        builder.Services.AddSingleton(registry);
        builder.Services.AddSingleton<KeywordExecutor>();
        builder.Services.AddSingleton<TelemetryProvider>();
        builder.Services.AddSingleton<IRuntimeEngine, RuntimeEngine>();
        builder.Services.AddSingleton<IWorkflowParser, YamlWorkflowParser>();
        builder.Services.AddSingleton<WorkflowLoader>();
        builder.Services.AddSingleton<JsonReportGenerator>();
        builder.Services.AddSingleton<HtmlReportGenerator>();
        builder.Services.AddSingleton<CircuitBreaker>();
        builder.Services.AddHttpClient<HttpRequestKeyword>();

        builder.Services.AddSingleton<SecretMasker>();
        builder.Services.AddSingleton<ISecretProvider, EnvSecretProvider>();
        builder.Services.AddSingleton<ISecretProvider, FileSecretProvider>();
        builder.Services.AddSingleton<SecretResolver>();

        builder.Services.AddSingleton<WorkflowHookRunner>();
        builder.Services.AddSingleton<ProgressHook>();

        builder.Services.AddAutoFlowDatabase();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetRequiredService<IRuntimeEngine>());
        Assert.NotNull(host.Services.GetRequiredService<IWorkflowParser>());
        Assert.NotNull(host.Services.GetRequiredService<WorkflowLoader>());
        Assert.NotNull(host.Services.GetRequiredService<KeywordExecutor>());
        Assert.NotNull(host.Services.GetRequiredService<KeywordRegistry>());
        Assert.NotNull(host.Services.GetRequiredService<CircuitBreaker>());
        Assert.NotNull(host.Services.GetRequiredService<SecretResolver>());
        Assert.NotNull(host.Services.GetRequiredService<SecretMasker>());
        Assert.NotNull(host.Services.GetRequiredService<IExecutionRepository>());
        Assert.NotNull(host.Services.GetRequiredService<WorkflowHookRunner>());
    }

    [Fact]
    public void ConfigureServices_RegistersSecretProviders()
    {
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Logging.ClearProviders();

        var registry = new KeywordRegistry();

        builder.Services.AddSingleton(registry);
        builder.Services.AddSingleton<KeywordExecutor>();
        builder.Services.AddSingleton<IRuntimeEngine, RuntimeEngine>();
        builder.Services.AddSingleton<IWorkflowParser, YamlWorkflowParser>();
        builder.Services.AddSingleton<WorkflowLoader>();

        builder.Services.AddSingleton<SecretMasker>();
        builder.Services.AddSingleton<ISecretProvider, EnvSecretProvider>();
        builder.Services.AddSingleton<ISecretProvider, FileSecretProvider>();
        builder.Services.AddSingleton<SecretResolver>();

        using var host = builder.Build();

        var envProvider = host.Services.GetRequiredService<ISecretProvider>();
        Assert.NotNull(envProvider);

        var secretResolver = host.Services.GetRequiredService<SecretResolver>();
        Assert.NotNull(secretResolver);
    }

    [Fact]
    public void ConfigureServices_RegistersHooks()
    {
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Logging.ClearProviders();

        var registry = new KeywordRegistry();

        builder.Services.AddSingleton(registry);
        builder.Services.AddSingleton<KeywordExecutor>();
        builder.Services.AddSingleton<IRuntimeEngine, RuntimeEngine>();
        builder.Services.AddSingleton<IWorkflowParser, YamlWorkflowParser>();
        builder.Services.AddSingleton<WorkflowLoader>();

        builder.Services.AddSingleton<SecretMasker>();
        builder.Services.AddSingleton<ISecretProvider, EnvSecretProvider>();
        builder.Services.AddSingleton<ISecretProvider, FileSecretProvider>();
        builder.Services.AddSingleton<SecretResolver>();

        builder.Services.AddSingleton<WorkflowHookRunner>();
        builder.Services.AddSingleton<ProgressHook>();
        builder.Services.AddSingleton<IWorkflowLifecycleHook, BrowserCleanupHook>();

        using var host = builder.Build();

        var hookRunner = host.Services.GetRequiredService<WorkflowHookRunner>();
        Assert.NotNull(hookRunner);

        var progressHook = host.Services.GetRequiredService<ProgressHook>();
        Assert.NotNull(progressHook);
    }

    [Fact]
    public void ConfigureServices_RegistersReportGenerators()
    {
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Logging.ClearProviders();

        var registry = new KeywordRegistry();

        builder.Services.AddSingleton(registry);
        builder.Services.AddSingleton<KeywordExecutor>();
        builder.Services.AddSingleton<IRuntimeEngine, RuntimeEngine>();
        builder.Services.AddSingleton<IWorkflowParser, YamlWorkflowParser>();
        builder.Services.AddSingleton<WorkflowLoader>();

        builder.Services.AddSingleton<JsonReportGenerator>();
        builder.Services.AddSingleton<HtmlReportGenerator>();

        using var host = builder.Build();

        var jsonGenerator = host.Services.GetRequiredService<JsonReportGenerator>();
        Assert.NotNull(jsonGenerator);

        var htmlGenerator = host.Services.GetRequiredService<HtmlReportGenerator>();
        Assert.NotNull(htmlGenerator);
    }

    [Fact]
    public void ConfigureServices_RegistersHttpClientForHttpRequestKeyword()
    {
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Logging.ClearProviders();

        var registry = new KeywordRegistry();

        builder.Services.AddSingleton(registry);
        builder.Services.AddSingleton<KeywordExecutor>();
        builder.Services.AddSingleton<IRuntimeEngine, RuntimeEngine>();
        builder.Services.AddSingleton<IWorkflowParser, YamlWorkflowParser>();
        builder.Services.AddSingleton<WorkflowLoader>();
        builder.Services.AddHttpClient<HttpRequestKeyword>();

        using var host = builder.Build();

        Assert.NotNull(host.Services.GetRequiredService<HttpClient>());
    }

    [Fact]
    public void ConfigureServices_RegistersDatabaseRepository()
    {
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());
        builder.Logging.ClearProviders();

        var registry = new KeywordRegistry();

        builder.Services.AddSingleton(registry);
        builder.Services.AddSingleton<KeywordExecutor>();
        builder.Services.AddSingleton<IRuntimeEngine, RuntimeEngine>();
        builder.Services.AddSingleton<IWorkflowParser, YamlWorkflowParser>();
        builder.Services.AddSingleton<WorkflowLoader>();
        builder.Services.AddAutoFlowDatabase();

        using var host = builder.Build();

        var repository = host.Services.GetRequiredService<IExecutionRepository>();
        Assert.NotNull(repository);
    }
}

public sealed class CliHelperFunctionTests
{
    [Fact]
    public void CountSteps_StepNode_Returns1()
    {
        var node = new StepNode { Id = "test" };
        var steps = new List<IWorkflowNode> { node };

        var result = CountSteps(steps);

        Assert.Equal(1, result);
    }

    [Fact]
    public void CountSteps_IfNode_CountsThenAndElse()
    {
        var node = new IfNode
        {
            Then = new List<IWorkflowNode> { new StepNode { Id = "then1" } },
            Else = new List<IWorkflowNode> { new StepNode { Id = "else1" }, new StepNode { Id = "else2" } }
        };
        var steps = new List<IWorkflowNode> { node };

        var result = CountSteps(steps);

        Assert.Equal(3, result);
    }

    [Fact]
    public void CountSteps_ForEachNode_CountsStepsInLoop()
    {
        var node = new ForEachNode
        {
            Steps = new List<IWorkflowNode>
            {
                new StepNode { Id = "step1" },
                new StepNode { Id = "step2" }
            }
        };
        var steps = new List<IWorkflowNode> { node };

        var result = CountSteps(steps);

        Assert.Equal(2, result);
    }

    [Fact]
    public void CountSteps_ParallelNode_CountsSteps()
    {
        var node = new ParallelNode
        {
            Steps = new List<IWorkflowNode>
            {
                new StepNode { Id = "step1" },
                new StepNode { Id = "step2" },
                new StepNode { Id = "step3" }
            }
        };
        var steps = new List<IWorkflowNode> { node };

        var result = CountSteps(steps);

        Assert.Equal(3, result);
    }

    [Fact]
    public void CountSteps_CallNode_Returns1()
    {
        var node = new CallNode { Task = "imported_task" };
        var steps = new List<IWorkflowNode> { node };

        var result = CountSteps(steps);

        Assert.Equal(1, result);
    }

    [Fact]
    public void CountSteps_GroupNode_CountsSteps()
    {
        var node = new GroupNode
        {
            Steps = new List<IWorkflowNode>
            {
                new StepNode { Id = "step1" },
                new StepNode { Id = "step2" }
            }
        };
        var steps = new List<IWorkflowNode> { node };

        var result = CountSteps(steps);

        Assert.Equal(2, result);
    }

    [Fact]
    public void CountAllSteps_WithOnError_IncludesErrorSteps()
    {
        var task = new TaskNode
        {
            Steps = new List<IWorkflowNode> { new StepNode { Id = "main1" } },
            OnError = new OnErrorNode
            {
                Steps = new List<IWorkflowNode> { new StepNode { Id = "error1" } }
            }
        };

        var document = new WorkflowDocument
        {
            Tasks = new Dictionary<string, TaskNode> { ["main"] = task }
        };

        var result = CountAllSteps(document);

        Assert.Equal(2, result);
    }

    [Fact]
    public void CountAllSteps_WithFinally_IncludesFinallySteps()
    {
        var task = new TaskNode
        {
            Steps = new List<IWorkflowNode> { new StepNode { Id = "main1" } },
            Finally = new FinallyNode
            {
                Steps = new List<IWorkflowNode> { new StepNode { Id = "finally1" } }
            }
        };

        var document = new WorkflowDocument
        {
            Tasks = new Dictionary<string, TaskNode> { ["main"] = task }
        };

        var result = CountAllSteps(document);

        Assert.Equal(2, result);
    }

    [Fact]
    public void CountAllSteps_MultipleTasks_SumsAllSteps()
    {
        var task1 = new TaskNode
        {
            Steps = new List<IWorkflowNode> { new StepNode { Id = "task1_step1" } }
        };

        var task2 = new TaskNode
        {
            Steps = new List<IWorkflowNode>
            {
                new StepNode { Id = "task2_step1" },
                new StepNode { Id = "task2_step2" }
            }
        };

        var document = new WorkflowDocument
        {
            Tasks = new Dictionary<string, TaskNode>
            {
                ["task1"] = task1,
                ["task2"] = task2
            }
        };

        var result = CountAllSteps(document);

        Assert.Equal(3, result);
    }

    private static int CountSteps(List<IWorkflowNode> nodes)
    {
        var count = 0;
        foreach (var node in nodes)
        {
            count += node switch
            {
                StepNode => 1,
                IfNode ifNode => CountSteps(ifNode.Then) + CountSteps(ifNode.Else),
                ForEachNode forEach => CountSteps(forEach.Steps),
                ParallelNode parallel => parallel.Steps.Count,
                CallNode => 1,
                GroupNode group => CountSteps(group.Steps),
                _ => 0
            };
        }
        return count;
    }

    private static int CountAllSteps(WorkflowDocument document)
    {
        var count = 0;
        foreach (var task in document.Tasks.Values)
        {
            count += CountSteps(task.Steps);
            if (task.OnError is not null)
                count += CountSteps(task.OnError.Steps);
            if (task.Finally is not null)
                count += CountSteps(task.Finally.Steps);
        }
        return count;
    }
}
