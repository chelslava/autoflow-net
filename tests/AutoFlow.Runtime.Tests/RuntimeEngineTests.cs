// Этот код нужен для проверки минимального выполнения шага через runtime.
using AutoFlow.Abstractions;
using AutoFlow.Library.Assertions;
using AutoFlow.PluginModel;
using AutoFlow.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AutoFlow.Runtime.Tests;

public sealed class RuntimeEngineTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldRunMainTask()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var registry = new KeywordRegistry();
        services.AddSingleton(registry);
        services.AddSingleton<KeywordExecutor>();
        services.AddSingleton<IRuntimeEngine, RuntimeEngine>();

        services.AddKeywordsFromAssembly(typeof(LogInfoKeyword).Assembly, registry.Register);

        using var provider = services.BuildServiceProvider();

        var runtime = provider.GetRequiredService<IRuntimeEngine>();

        var document = new WorkflowDocument
        {
            Name = "demo",
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
                                ["message"] = "Тест"
                            }
                        }
                    ]
                }
            }
        };

        var result = await runtime.ExecuteAsync(document, new RuntimeLaunchOptions());

        Assert.Equal(ExecutionStatus.Passed, result.Status);
        Assert.Single(result.Steps);
        Assert.Equal("log_start", result.Steps[0].StepId);
    }
}
