// Этот код нужен для стартовой CLI-точки входа.
// Он поднимает DI, регистрирует runtime и запускает минимальный flow.
using AutoFlow.Abstractions;
using AutoFlow.Library.Assertions;
using AutoFlow.Parser;
using AutoFlow.PluginModel;
using AutoFlow.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSingleton<KeywordRegistry>();
builder.Services.AddSingleton<KeywordExecutor>();
builder.Services.AddSingleton<IRuntimeEngine, RuntimeEngine>();
builder.Services.AddSingleton<IWorkflowParser, YamlWorkflowParser>();

using var host = builder.Build();

var registry = host.Services.GetRequiredService<KeywordRegistry>();
builder.Services.AddKeywordsFromAssembly(typeof(LogInfoKeyword).Assembly, registry.Register);

var fileArgument = new Argument<FileInfo>(
    name: "file",
    description: "Путь к YAML-файлу workflow.");

var runCommand = new Command("run", "Выполняет workflow-файл.");
runCommand.AddArgument(fileArgument);

runCommand.SetHandler(async (FileInfo file) =>
{
    if (!file.Exists)
        throw new FileNotFoundException("Файл workflow не найден.", file.FullName);

    var parser = host.Services.GetRequiredService<IWorkflowParser>();
    var runtime = host.Services.GetRequiredService<IRuntimeEngine>();

    var yaml = await File.ReadAllTextAsync(file.FullName);
    var document = parser.Parse(yaml);

    var result = await runtime.ExecuteAsync(
        document,
        new RuntimeLaunchOptions());

    Console.WriteLine($"Workflow: {result.WorkflowName}");
    Console.WriteLine($"Статус: {result.Status}");
    Console.WriteLine($"Шагов выполнено: {result.Steps.Count}");
}, fileArgument);

var rootCommand = new RootCommand("AutoFlow.NET CLI");
rootCommand.AddCommand(runCommand);

return await rootCommand.InvokeAsync(args);
