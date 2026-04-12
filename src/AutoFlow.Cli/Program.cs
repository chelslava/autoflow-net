using AutoFlow.Abstractions;
using AutoFlow.Library.Assertions;
using AutoFlow.Library.Files;
using AutoFlow.Library.Http;
using AutoFlow.Parser;
using AutoFlow.Reporting;
using AutoFlow.Runtime;
using AutoFlow.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var registry = new KeywordRegistry();

builder.Services.AddSingleton(registry);
builder.Services.AddSingleton<KeywordExecutor>();
builder.Services.AddSingleton<IRuntimeEngine, RuntimeEngine>();
builder.Services.AddSingleton<IWorkflowParser, YamlWorkflowParser>();
builder.Services.AddSingleton<WorkflowLoader>();
builder.Services.AddSingleton<JsonReportGenerator>();
builder.Services.AddHttpClient<HttpRequestKeyword>();

builder.Services.AddKeywordsFromAssembly(
    typeof(LogInfoKeyword).Assembly,
    (name, handlerType, argsType, category, description) =>
        registry.Register(name, handlerType, argsType, category, description));

builder.Services.AddKeywordsFromAssembly(
    typeof(FileReadKeyword).Assembly,
    (name, handlerType, argsType, category, description) =>
        registry.Register(name, handlerType, argsType, category, description));

builder.Services.AddKeywordsFromAssembly(
    typeof(HttpRequestKeyword).Assembly,
    (name, handlerType, argsType, category, description) =>
        registry.Register(name, handlerType, argsType, category, description));

using var host = builder.Build();

var fileArgument = new Argument<FileInfo>(
    name: "file",
    description: "Путь к YAML-файлу workflow.");

var outputOption = new Option<FileInfo?>(
    name: "--output",
    description: "Путь для сохранения JSON-отчёта.");

var validateCommand = new Command("validate", "Валидирует workflow-файл без выполнения.");
validateCommand.AddArgument(fileArgument);

validateCommand.SetHandler((FileInfo file) =>
{
    if (!file.Exists)
    {
        Console.WriteLine($"Ошибка: Файл не найден: {file.FullName}");
        return;
    }

    var parser = host.Services.GetRequiredService<IWorkflowParser>();
    var yaml = File.ReadAllText(file.FullName);
    var document = parser.Parse(yaml);

    var validator = new WorkflowValidator(registry);
    var result = validator.Validate(document);

    if (result.IsValid)
    {
        Console.WriteLine($"✓ Валидация пройдена: {file.Name}");
    }
    else
    {
        Console.WriteLine($"✗ Валидация не пройдена:");
        foreach (var error in result.Errors)
        {
            Console.WriteLine($"  [{error.Code}] {error.Message}");
            if (error.Location is not null)
                Console.WriteLine($"    Location: {error.Location}");
            if (error.Suggestion is not null)
                Console.WriteLine($"    Suggestion: {error.Suggestion}");
        }
    }
}, fileArgument);

var runCommand = new Command("run", "Выполняет workflow-файл.");
runCommand.AddArgument(fileArgument);
runCommand.AddOption(outputOption);

runCommand.SetHandler(async (FileInfo file, FileInfo? output) =>
{
    if (!file.Exists)
    {
        Console.WriteLine($"Ошибка: Файл не найден: {file.FullName}");
        return;
    }

    var loader = host.Services.GetRequiredService<WorkflowLoader>();
    var runtime = host.Services.GetRequiredService<IRuntimeEngine>();
    var reportGenerator = host.Services.GetRequiredService<JsonReportGenerator>();

    WorkflowDocument document;
    try
    {
        document = loader.LoadFromFile(file.FullName);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка загрузки workflow: {ex.Message}");
        return;
    }

    var validator = new WorkflowValidator(registry);
    var validationResult = validator.Validate(document);

    if (!validationResult.IsValid)
    {
        Console.WriteLine($"✗ Валидация не пройдена:");
        foreach (var error in validationResult.Errors)
        {
            Console.WriteLine($"  [{error.Code}] {error.Message}");
        }
        return;
    }

    Console.WriteLine($"→ Выполняется: {document.Name}");

    var result = await runtime.ExecuteAsync(
        document,
        new RuntimeLaunchOptions());

    var statusIcon = result.Status == ExecutionStatus.Passed ? "✓" : "✗";
    Console.WriteLine($"{statusIcon} Workflow: {result.WorkflowName}");
    Console.WriteLine($"  Статус: {result.Status}");
    Console.WriteLine($"  Шагов: {result.Steps.Count}");
    Console.WriteLine($"  Длительность: {result.Duration.TotalMilliseconds}ms");

    foreach (var step in result.Steps)
    {
        var stepIcon = step.Status == ExecutionStatus.Passed ? "  ✓" : "  ✗";
        Console.WriteLine($"{stepIcon} {step.StepId}: {step.KeywordName} ({step.Duration.TotalMilliseconds}ms)");
        if (step.ErrorMessage is not null)
            Console.WriteLine($"     Error: {step.ErrorMessage}");
    }

    if (output is not null)
    {
        var json = reportGenerator.Generate(result);
        await File.WriteAllTextAsync(output.FullName, json);
        Console.WriteLine($"  Отчёт: {output.FullName}");
    }
}, fileArgument, outputOption);

var listKeywordsCommand = new Command("list-keywords", "Выводит список доступных keywords.");
listKeywordsCommand.SetHandler(() =>
{
    Console.WriteLine("Доступные keywords:");
    foreach (var keyword in registry.GetAll())
    {
        Console.WriteLine($"  {keyword.Name}");
        if (keyword.Description is not null)
            Console.WriteLine($"    {keyword.Description}");
        if (keyword.Category is not null)
            Console.WriteLine($"    Category: {keyword.Category}");
    }
});

var rootCommand = new RootCommand("AutoFlow.NET CLI");
rootCommand.AddCommand(runCommand);
rootCommand.AddCommand(validateCommand);
rootCommand.AddCommand(listKeywordsCommand);

return await rootCommand.InvokeAsync(args);
