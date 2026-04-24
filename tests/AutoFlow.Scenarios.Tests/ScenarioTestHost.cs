using AutoFlow.Abstractions;
using AutoFlow.Library.Assertions;
using AutoFlow.Library.Files;
using AutoFlow.Library.Http;
using AutoFlow.Parser;
using AutoFlow.Reporting;
using AutoFlow.Runtime;
using AutoFlow.Runtime.Resilience;
using AutoFlow.Runtime.Secrets;
using AutoFlow.Runtime.Telemetry;
using AutoFlow.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Scenarios.Tests;

internal sealed class ScenarioTestHost : IDisposable
{
    private ScenarioTestHost(ServiceProvider serviceProvider, KeywordRegistry registry)
    {
        Services = serviceProvider;
        Registry = registry;
        Loader = serviceProvider.GetRequiredService<WorkflowLoader>();
        Runtime = serviceProvider.GetRequiredService<IRuntimeEngine>();
        JsonReportGenerator = serviceProvider.GetRequiredService<JsonReportGenerator>();
        HtmlReportGenerator = serviceProvider.GetRequiredService<HtmlReportGenerator>();
    }

    public ServiceProvider Services { get; }
    public KeywordRegistry Registry { get; }
    public WorkflowLoader Loader { get; }
    public IRuntimeEngine Runtime { get; }
    public JsonReportGenerator JsonReportGenerator { get; }
    public HtmlReportGenerator HtmlReportGenerator { get; }

    public static ScenarioTestHost Create()
    {
        var services = new ServiceCollection();
        var registry = new KeywordRegistry();

        services.AddLogging(builder => builder.ClearProviders());
        services.AddSingleton(registry);
        services.AddSingleton<KeywordExecutor>();
        services.AddSingleton<TelemetryProvider>();
        services.AddSingleton<IRuntimeEngine, RuntimeEngine>();
        services.AddSingleton<IWorkflowParser, YamlWorkflowParser>();
        services.AddSingleton<WorkflowLoader>();
        services.AddSingleton<JsonReportGenerator>();
        services.AddSingleton<HtmlReportGenerator>();
        services.AddSingleton<CircuitBreaker>();
        services.AddHttpClient<HttpRequestKeyword>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 100
            });

        services.AddSingleton<SecretMasker>();
        services.AddSingleton<ISecretProvider, EnvSecretProvider>();
        services.AddSingleton<ISecretProvider, FileSecretProvider>();
        services.AddSingleton<SecretResolver>();
        services.AddSingleton<WorkflowHookRunner>();

        services.AddKeywordsFromAssembly(
            typeof(LogInfoKeyword).Assembly,
            (name, handlerType, argsType, category, description) =>
                registry.Register(name, handlerType, argsType, category, description));

        services.AddKeywordsFromAssembly(
            typeof(FileReadKeyword).Assembly,
            (name, handlerType, argsType, category, description) =>
                registry.Register(name, handlerType, argsType, category, description));

        services.AddKeywordsFromAssembly(
            typeof(HttpRequestKeyword).Assembly,
            (name, handlerType, argsType, category, description) =>
                registry.Register(name, handlerType, argsType, category, description));

        return new ScenarioTestHost(services.BuildServiceProvider(), registry);
    }

    public void Dispose()
    {
        Services.Dispose();
    }
}

internal static class ScenarioTestHarness
{
    private static readonly SemaphoreSlim RepoLock = new(1, 1);

    public static string RepoRoot { get; } = FindRepoRoot();

    public static async Task RunInRepoAsync(Func<Task> action)
    {
        await RepoLock.WaitAsync().ConfigureAwait(false);
        var currentDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(RepoRoot);
            await action().ConfigureAwait(false);
        }
        finally
        {
            Directory.SetCurrentDirectory(currentDirectory);
            RepoLock.Release();
        }
    }

    public static string GetPath(params string[] segments) => Path.Join([RepoRoot, .. segments]);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Join(directory.FullName, "AutoFlow.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing AutoFlow.sln.");
    }
}
