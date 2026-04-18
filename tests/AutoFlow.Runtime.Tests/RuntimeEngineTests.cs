// =============================================================================
// RuntimeEngineTests.cs — тесты для RuntimeEngine.
//
// Проверяет выполнение workflow с минимальным набором сервисов.
// =============================================================================

using AutoFlow.Abstractions;
using AutoFlow.Library.Assertions;
using AutoFlow.Runtime;
using AutoFlow.Runtime.Secrets;
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

        // Новые зависимости
        services.AddSingleton<SecretMasker>();
        services.AddSingleton<ISecretProvider, EnvSecretProvider>();
        services.AddSingleton<ISecretProvider, FileSecretProvider>();
        services.AddSingleton<SecretResolver>();
        services.AddSingleton<WorkflowHookRunner>();

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
        var steps = result.Steps;
        Assert.Equal("log_start", steps[0].StepId);
    }
}
