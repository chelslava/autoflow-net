// =============================================================================
// Program.cs — CLI entry point для AutoFlow.NET.
//
// Регистрирует все сервисы, hooks, secret providers и обрабатывает команды.
// =============================================================================

using AutoFlow.Abstractions;
using AutoFlow.Database;
using AutoFlow.Library.Assertions;
using AutoFlow.Library.Browser;
using AutoFlow.Library.Files;
using AutoFlow.Library.Http;
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
using System.CommandLine;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var registry = new KeywordRegistry();

// Core services
builder.Services.AddSingleton(registry);
builder.Services.AddSingleton<KeywordExecutor>();
builder.Services.AddSingleton<TelemetryProvider>();
builder.Services.AddSingleton<IRuntimeEngine, RuntimeEngine>();
builder.Services.AddSingleton<IWorkflowParser, YamlWorkflowParser>();
builder.Services.AddSingleton<WorkflowLoader>();
builder.Services.AddSingleton<JsonReportGenerator>();
builder.Services.AddSingleton<HtmlReportGenerator>();
builder.Services.AddSingleton<CircuitBreaker>();
builder.Services.AddHttpClient<HttpRequestKeyword>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        MaxConnectionsPerServer = 100
    });

// Browser manager - centralized lifecycle management
builder.Services.AddSingleton<BrowserManager>();

// Secret management
builder.Services.AddSingleton<SecretMasker>();
builder.Services.AddSingleton<ISecretProvider, EnvSecretProvider>();
builder.Services.AddSingleton<ISecretProvider, FileSecretProvider>();
builder.Services.AddSingleton<SecretResolver>();

// Lifecycle hooks - регистрируем все реализации из DI
builder.Services.AddSingleton<WorkflowHookRunner>();
builder.Services.AddSingleton<ProgressHook>();
builder.Services.AddSingleton<IWorkflowLifecycleHook, ProgressHook>(sp => sp.GetRequiredService<ProgressHook>());
builder.Services.AddSingleton<IWorkflowLifecycleHook, BrowserCleanupHook>();

// Database
builder.Services.AddAutoFlowDatabase();

// Keywords
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

builder.Services.AddKeywordsFromAssembly(
    typeof(BrowserOpenKeyword).Assembly,
    (name, handlerType, argsType, category, description) =>
        registry.Register(name, handlerType, argsType, category, description));

using var host = builder.Build();

#pragma warning disable CS0618 // Type or member is obsolete
var browserManager = host.Services.GetRequiredService<BrowserManager>();
BrowserManagerProvider.Initialize(browserManager);
#pragma warning restore CS0618

var fileArgument = new Argument<FileInfo>(
    name: "file",
    description: "Путь к YAML-файлу workflow.");

var outputOption = new Option<FileInfo?>(
    name: "--output",
    description: "Путь для сохранения отчёта (формат определяется по расширению: .json или .html).");

var outputFormatOption = new Option<string?>(
    name: "--format",
    description: "Формат отчёта: json или html (по умолчанию определяется по расширению файла).");

var runIdOption = new Option<string?>(
    name: "--run-id",
    description: "Уникальный идентификатор запуска (генерируется автоматически, если не указан).");

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

    WorkflowDocument document;
    try
    {
        document = parser.Parse(yaml);
    }
    catch (YamlDotNet.Core.YamlException yamlEx)
    {
        Console.WriteLine($"✗ YAML parse error:");
        Console.WriteLine($"  Line {yamlEx.Start.Line}, Column {yamlEx.Start.Column}: {yamlEx.Message}");
        return;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Parse error: {ex.Message}");
        return;
    }

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
runCommand.AddOption(outputFormatOption);
runCommand.AddOption(runIdOption);

var dryRunOption = new Option<bool>(
    name: "--dry-run",
    description: "Validate and show execution plan without actually running.");
runCommand.AddOption(dryRunOption);

runCommand.SetHandler(async (FileInfo file, FileInfo? output, string? format, string? runId, bool dryRun) =>
{
    if (!file.Exists)
    {
        Console.WriteLine($"Ошибка: Файл не найден: {file.FullName}");
        return;
    }

    var loader = host.Services.GetRequiredService<WorkflowLoader>();
    var jsonReportGenerator = host.Services.GetRequiredService<JsonReportGenerator>();
    var htmlReportGenerator = host.Services.GetRequiredService<HtmlReportGenerator>();

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

    if (dryRun)
    {
        Console.WriteLine($"🔍 Dry run: {document.Name}");
        Console.WriteLine($"  Variables: {document.Variables.Count}");
        foreach (var (key, value) in document.Variables)
        {
            var displayValue = key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                               key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                               key.Contains("token", StringComparison.OrdinalIgnoreCase)
                ? "***"
                : value?.ToString() ?? "null";
            Console.WriteLine($"    {key} = {displayValue}");
        }

        Console.WriteLine($"  Tasks: {document.Tasks.Count}");
        foreach (var (taskName, task) in document.Tasks)
        {
            Console.WriteLine($"    {taskName}: {CountSteps(task.Steps)} steps");
            PrintExecutionPlan(task.Steps, 2);
        }

        Console.WriteLine();
        Console.WriteLine("✓ Dry run completed successfully. Workflow is valid and ready to execute.");
        return;
    }

    var runtime = host.Services.GetRequiredService<IRuntimeEngine>();
    var progressHook = host.Services.GetRequiredService<ProgressHook>();
    
    var totalSteps = CountAllSteps(document);
    progressHook.SetTotalSteps(totalSteps);

    Console.WriteLine($"→ Выполняется: {document.Name} ({totalSteps} steps)");
    if (!string.IsNullOrEmpty(runId))
        Console.WriteLine($"  Run ID: {runId}");
    Console.WriteLine("  Press Ctrl+C to cancel...");

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (sender, e) =>
    {
        e.Cancel = true;
        Console.WriteLine("\nCancellation requested...");
        cts.Cancel();
    };

    RunResult result;
    try
    {
        result = await runtime.ExecuteAsync(
            document,
            new RuntimeLaunchOptions { RunId = runId },
            cts.Token);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine();
        Console.WriteLine("✗ Workflow cancelled by user");
        return;
    }

    var statusIcon = result.Status == ExecutionStatus.Passed ? "✓" : "✗";
    Console.WriteLine();
    Console.WriteLine($"{statusIcon} Workflow: {result.WorkflowName}");
    Console.WriteLine($"  Статус: {result.Status}");
    Console.WriteLine($"  Длительность: {result.Duration.TotalMilliseconds}ms");

    if (output is not null)
    {
        var outputPath = output.FullName;
        var workingDirectory = Directory.GetCurrentDirectory();
        var normalizedOutput = Path.GetFullPath(outputPath);
        
        if (!normalizedOutput.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Ошибка: --output должен быть внутри рабочей директории: {workingDirectory}");
            return;
        }
        
        if (normalizedOutput.Contains(".."))
        {
            Console.WriteLine("Ошибка: --output не может содержать path traversal (../)");
            return;
        }
        
        var reportFormat = DetermineReportFormat(output.FullName, format);
        var reportContent = reportFormat == "html"
            ? htmlReportGenerator.Generate(result)
            : jsonReportGenerator.Generate(result);
        
        var outputDir = Path.GetDirectoryName(normalizedOutput);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);
        
        await File.WriteAllTextAsync(normalizedOutput, reportContent);
        Console.WriteLine($"  Отчёт ({reportFormat}): {normalizedOutput}");
    }
}, fileArgument, outputOption, outputFormatOption, runIdOption, dryRunOption);

static int CountSteps(List<IWorkflowNode> nodes)
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

static int CountAllSteps(WorkflowDocument document)
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

static void PrintExecutionPlan(List<IWorkflowNode> nodes, int indent)
{
    var prefix = new string(' ', indent * 2);
    foreach (var node in nodes)
    {
        switch (node)
        {
            case StepNode step:
                var retryInfo = step.Retry is not null ? $" (retry: {step.Retry.Attempts}x)" : "";
                Console.WriteLine($"{prefix}- [{step.Id}] {step.Uses}{retryInfo}");
                break;
            case IfNode ifNode:
                Console.WriteLine($"{prefix}? IF condition");
                PrintExecutionPlan(ifNode.Then, indent + 1);
                if (ifNode.Else.Count > 0)
                {
                    Console.WriteLine($"{prefix}? ELSE");
                    PrintExecutionPlan(ifNode.Else, indent + 1);
                }
                break;
            case ForEachNode forEach:
                Console.WriteLine($"{prefix}* FOR_EACH as {forEach.As}");
                PrintExecutionPlan(forEach.Steps, indent + 1);
                break;
            case ParallelNode parallel:
                Console.WriteLine($"{prefix}|| PARALLEL (max: {parallel.MaxConcurrency}) [{parallel.Steps.Count} steps]");
                for (var i = 0; i < parallel.Steps.Count; i++)
                {
                    var parallelNode = parallel.Steps[i];
                    var isLast = i == parallel.Steps.Count - 1;
                    var connector = isLast ? "└─" : "├─";
                    Console.WriteLine($"{prefix}  {connector} [{GetStepId(parallelNode)}]");
                }
                break;
            case CallNode call:
                Console.WriteLine($"{prefix}> CALL {call.Task}");
                break;
            case GroupNode group:
                Console.WriteLine($"{prefix}# GROUP: {group.Name}");
                PrintExecutionPlan(group.Steps, indent + 1);
                break;
        }
    }
}

static string DetermineReportFormat(string filePath, string? explicitFormat)
{
    if (!string.IsNullOrEmpty(explicitFormat))
        return explicitFormat.ToLowerInvariant();
    
    var extension = Path.GetExtension(filePath).ToLowerInvariant();
    return extension == ".html" ? "html" : "json";
}

static string GetStepId(IWorkflowNode node) => node switch
{
    StepNode step => !string.IsNullOrEmpty(step.Id) ? step.Id : step.Uses ?? "unnamed",
    IfNode => "if-condition",
    ForEachNode forEach => $"for-each as {forEach.As}",
    ParallelNode => "parallel",
    CallNode call => $"call {call.Task}",
    GroupNode group => group.Name,
    _ => "unknown"
};

var listKeywordsCommand = new Command("list-keywords", "Выводит список доступных keywords.");
var listKeywordsOutputOption = new Option<string?>(
    name: "--output",
    description: "Output format: text (default) or json.");

listKeywordsCommand.AddOption(listKeywordsOutputOption);

listKeywordsCommand.SetHandler((string? outputFormat) =>
{
    var keywords = registry.GetAll();
    
    if (outputFormat?.Equals("json", StringComparison.OrdinalIgnoreCase) == true)
    {
        var jsonData = keywords.Select(k => new
        {
            name = k.Name,
            description = k.Description,
            category = k.Category,
            argsType = k.ArgsType?.Name
        });
        var jsonOutput = System.Text.Json.JsonSerializer.Serialize(jsonData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        Console.WriteLine(jsonOutput);
        return;
    }
    
    Console.WriteLine("Доступные keywords:");
    foreach (var keyword in keywords)
    {
        Console.WriteLine($"  {keyword.Name}");
        if (keyword.Description is not null)
            Console.WriteLine($"    {keyword.Description}");
        if (keyword.Category is not null)
            Console.WriteLine($"    Category: {keyword.Category}");
    }
}, listKeywordsOutputOption);

var historyCommand = new Command("history", "Показывает историю выполнений.");
var historyLimitOption = new Option<int>(
    name: "--limit",
    description: "Максимальное количество записей.",
    getDefaultValue: () => 20);
var historyWorkflowOption = new Option<string?>(
    name: "--workflow",
    description: "Фильтр по имени workflow.");
var historyStatusOption = new Option<string?>(
    name: "--status",
    description: "Фильтр по статусу (Passed, Failed, etc.).");
var historyOutputFormatOption = new Option<string?>(
    name: "--output",
    description: "Output format: text (default) or json.");

historyCommand.AddOption(historyLimitOption);
historyCommand.AddOption(historyWorkflowOption);
historyCommand.AddOption(historyStatusOption);
historyCommand.AddOption(historyOutputFormatOption);

historyCommand.SetHandler(async (int limit, string? workflow, string? status, string? outputFormat) =>
{
    var repository = host.Services.GetRequiredService<IExecutionRepository>();
    var records = await repository.GetListAsync(workflow, status, limit);

    if (outputFormat?.Equals("json", StringComparison.OrdinalIgnoreCase) == true)
    {
        var jsonOutput = System.Text.Json.JsonSerializer.Serialize(records, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        Console.WriteLine(jsonOutput);
        return;
    }

    if (records.Count == 0)
    {
        Console.WriteLine("История пуста.");
        return;
    }

    Console.WriteLine($"История выполнений ({records.Count} записей):");
    Console.WriteLine();

    foreach (var record in records)
    {
        var icon = record.Status == "Passed" ? "✓" : "✗";
        Console.WriteLine($"{icon} [{record.RunId[..8]}] {record.WorkflowName}");
        Console.WriteLine($"   Status: {record.Status} | Duration: {record.DurationMs}ms | Steps: {record.StepsPassed}/{record.StepsTotal}");
        Console.WriteLine($"   Started: {record.StartedAtUtc}");
        if (record.ErrorMessage is not null)
            Console.WriteLine($"   Error: {record.ErrorMessage[..Math.Min(100, record.ErrorMessage.Length)]}...");
        Console.WriteLine();
    }
}, historyLimitOption, historyWorkflowOption, historyStatusOption, historyOutputFormatOption);

var showCommand = new Command("show", "Показывает детали выполнения по RunId.");
var showRunIdArgument = new Argument<string>(
    name: "run-id",
    description: "RunId выполнения.");

showCommand.AddArgument(showRunIdArgument);

showCommand.SetHandler(async (string runId) =>
{
    var repository = host.Services.GetRequiredService<IExecutionRepository>();
    var record = await repository.GetByRunIdAsync(runId);

    if (record is null)
    {
        Console.WriteLine($"Выполнение с RunId '{runId}' не найдено.");
        return;
    }

    var icon = record.Status == "Passed" ? "✓" : "✗";
    Console.WriteLine($"{icon} {record.WorkflowName}");
    Console.WriteLine($"   RunId: {record.RunId}");
    Console.WriteLine($"   Status: {record.Status}");
    Console.WriteLine($"   Started: {record.StartedAtUtc}");
    Console.WriteLine($"   Finished: {record.FinishedAtUtc}");
    Console.WriteLine($"   Duration: {record.DurationMs}ms");
    Console.WriteLine($"   Steps: {record.StepsPassed}/{record.StepsTotal} passed");
    Console.WriteLine();

    if (record.ErrorMessage is not null)
    {
        Console.WriteLine($"   Error: {record.ErrorMessage}");
        Console.WriteLine();
    }

    if (record.StepsJson is not null)
    {
        Console.WriteLine("   Steps:");
        var steps = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<StepExecutionResult>>(record.StepsJson);
        if (steps is not null)
        {
            foreach (var step in steps)
            {
                var stepIcon = step.Status == ExecutionStatus.Passed ? "     ✓" : "     ✗";
                Console.WriteLine($"{stepIcon} {step.StepId}: {step.KeywordName} ({step.Duration.TotalMilliseconds}ms)");
                if (step.ErrorMessage is not null)
                    Console.WriteLine($"       Error: {step.ErrorMessage}");
            }
        }
    }
}, showRunIdArgument);

var statsCommand = new Command("stats", "Показывает статистику по выполнениям.");
var statsWorkflowOption = new Option<string?>(
    name: "--workflow",
    description: "Фильтр по имени workflow.");
var statsDaysOption = new Option<int>(
    name: "--days",
    description: "Период в днях.",
    getDefaultValue: () => 30);
var statsOutputFormatOption = new Option<string?>(
    name: "--output",
    description: "Output format: text (default) or json.");

statsCommand.AddOption(statsWorkflowOption);
statsCommand.AddOption(statsDaysOption);
statsCommand.AddOption(statsOutputFormatOption);

statsCommand.SetHandler(async (string? workflow, int days, string? outputFormat) =>
{
    var repository = host.Services.GetRequiredService<IExecutionRepository>();
    var from = DateTimeOffset.UtcNow.AddDays(-days);
    var stats = await repository.GetStatisticsAsync(workflow, from);

    if (outputFormat?.Equals("json", StringComparison.OrdinalIgnoreCase) == true)
    {
        var jsonOutput = System.Text.Json.JsonSerializer.Serialize(stats, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        Console.WriteLine(jsonOutput);
        return;
    }

    Console.WriteLine($"Статистика за последние {days} дней:");
    if (workflow is not null)
        Console.WriteLine($"   Workflow: {workflow}");
    Console.WriteLine();
    Console.WriteLine($"   Всего запусков: {stats.TotalRuns}");
    Console.WriteLine($"   Успешных: {stats.PassedRuns} ({stats.SuccessRate:F1}%)");
    Console.WriteLine($"   Failed: {stats.FailedRuns}");
    Console.WriteLine($"   Средняя длительность: {stats.AverageDurationMs:F0}ms");
    Console.WriteLine($"   Min/Max длительность: {stats.MinDurationMs}ms / {stats.MaxDurationMs}ms");
    Console.WriteLine($"   Всего шагов: {stats.TotalSteps}");
}, statsWorkflowOption, statsDaysOption, statsOutputFormatOption);

var cleanCommand = new Command("clean", "Удаляет старые записи из истории.");
var cleanDaysOption = new Option<int>(
    name: "--older-than",
    description: "Удалить записи старше указанного количества дней.",
    getDefaultValue: () => 30);

cleanCommand.AddOption(cleanDaysOption);

cleanCommand.SetHandler(async (int olderThan) =>
{
    var repository = host.Services.GetRequiredService<IExecutionRepository>();
    var deleted = await repository.DeleteOlderThanAsync(olderThan);

    Console.WriteLine($"Удалено {deleted} записей старше {olderThan} дней.");
}, cleanDaysOption);

var newCommand = new Command("new", "Создаёт новый workflow из шаблона.");
var newNameArgument = new Argument<string>(
    name: "name",
    description: "Имя workflow файла (без расширения).");

var templateOption = new Option<string>(
    name: "--template",
    description: "Шаблон: http, browser, parallel, files",
    getDefaultValue: () => "basic");

newCommand.AddArgument(newNameArgument);
newCommand.AddOption(templateOption);

newCommand.SetHandler((string name, string template) =>
{
    var fileName = name.EndsWith(".yaml") ? name : $"{name}.yaml";
    
    var content = template.ToLowerInvariant() switch
    {
        "http" => @"schema_version: 1
name: http_example

variables:
  api_base: https://api.example.com

tasks:
  main:
    steps:
      - step:
          id: fetch_data
          uses: http.request
          with:
            url: ""${api_base}/data""
            method: GET
          save_as:
            body: response_data

      - step:
          id: parse_response
          uses: json.parse
          with:
            json: ""${response_data}""
            path: ""results""
          save_as:
            value: results
",
        "browser" => @"schema_version: 1
name: browser_example

tasks:
  main:
    steps:
      - step:
          id: open_browser
          uses: browser.open
          with:
            browser: chromium
            headless: true
          save_as:
            browserId: browser_id

      - step:
          id: navigate
          uses: browser.goto
          with:
            browserId: ""${browser_id}""
            url: https://example.com

      - step:
          id: get_title
          uses: browser.get_text
          with:
            browserId: ""${browser_id}""
            selector: h1
          save_as:
            text: page_title

      - step:
          id: close
          uses: browser.close
          with:
            browserId: ""${browser_id}""
",
        "parallel" => @"schema_version: 1
name: parallel_example

variables:
  api_base: https://api.example.com

tasks:
  main:
    steps:
      - parallel:
          id: fetch_all
          max_concurrency: 3
          steps:
            - step:
                id: fetch_users
                uses: http.request
                with:
                  url: ""${api_base}/users""
                save_as:
                  body: users_data

            - step:
                id: fetch_posts
                uses: http.request
                with:
                  url: ""${api_base}/posts""
                save_as:
                  body: posts_data

            - step:
                id: fetch_comments
                uses: http.request
                with:
                  url: ""${api_base}/comments""
                save_as:
                  body: comments_data
",
        "files" => @"schema_version: 1
name: files_example

variables:
  input_file: ./data/input.txt
  output_file: ./data/output.txt

tasks:
  main:
    steps:
      - step:
          id: read_file
          uses: files.read
          with:
            path: ""${input_file}""
          save_as:
            content: file_content

      - step:
          id: write_file
          uses: files.write
          with:
            path: ""${output_file}""
            content: ""${file_content}""
",
        _ => @"schema_version: 1
name: basic_example

variables:
  message: Hello, World!

tasks:
  main:
    steps:
      - step:
          id: log_message
          uses: log.info
          with:
            message: ""${message}""
"
    };

    File.WriteAllText(fileName, content);
    Console.WriteLine($"✓ Created: {fileName}");
    Console.WriteLine($"  Template: {template}");
    Console.WriteLine();
    Console.WriteLine("Edit the file and run with:");
    Console.WriteLine($"  dotnet run --project src/AutoFlow.Cli -- run {fileName}");
}, newNameArgument, templateOption);

var graphCommand = new Command("graph", "Generates workflow dependency visualization (Mermaid format).");
var graphFileArgument = new Argument<FileInfo>(
    name: "file",
    description: "Workflow YAML file.");

graphCommand.AddArgument(graphFileArgument);

graphCommand.SetHandler((FileInfo file) =>
{
    if (!file.Exists)
    {
        Console.WriteLine($"✗ File not found: {file.FullName}");
        return;
    }

    try
    {
        var parser = host.Services.GetRequiredService<IWorkflowParser>();
        var yaml = File.ReadAllText(file.FullName);
        var document = parser.Parse(yaml);
        
        Console.WriteLine("```mermaid");
        Console.WriteLine("flowchart TD");
        Console.WriteLine($"    subgraph {document.Name}[{document.Name}]");
        
        foreach (var task in document.Tasks)
        {
            Console.WriteLine($"        subgraph {task.Name}[Task: {task.Name}]");
            GenerateStepsGraph(task.Steps, task.Name);
            Console.WriteLine("        end");
        }
        
        Console.WriteLine("    end");
        Console.WriteLine("```");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Parse error: {ex.Message}");
    }
}, graphFileArgument);

static void GenerateStepsGraph(List<IWorkflowNode> steps, string parent)
{
    for (var i = 0; i < steps.Count; i++)
    {
        var workflowNode = steps[i];
        var nodeId = $"{parent}_{i}";
        
        switch (workflowNode)
        {
            case StepNode step:
                var label = !string.IsNullOrEmpty(step.Id) ? step.Id : step.Uses ?? "unnamed";
                Console.WriteLine($"            {nodeId}[\"{label}\"]");
                break;
            case IfNode ifNode:
                Console.WriteLine($"            {nodeId}{{\"if: condition\"}}");
                GenerateStepsGraph(ifNode.Then, $"{nodeId}_then");
                if (ifNode.Else.Count > 0)
                    GenerateStepsGraph(ifNode.Else, $"{nodeId}_else");
                break;
            case ParallelNode parallel:
                Console.WriteLine($"            {nodeId}[/\"parallel x{parallel.Steps.Count}\"\\]");
                break;
            case ForEachNode forEach:
                Console.WriteLine($"            {nodeId}([\"for each: {forEach.As}\"])");
                break;
            case CallNode call:
                Console.WriteLine($"            {nodeId}[[\"call: {call.Task}\"]]");
                break;
            case GroupNode group:
                Console.WriteLine($"            {nodeId}(\"group: {group.Name}\")");
                break;
        }
        
        if (i > 0)
        {
            Console.WriteLine($"            {parent}_{i - 1} --> {nodeId}");
        }
    }
}

var rootCommand = new RootCommand("AutoFlow.NET CLI");
rootCommand.AddCommand(runCommand);
rootCommand.AddCommand(validateCommand);
rootCommand.AddCommand(listKeywordsCommand);
rootCommand.AddCommand(historyCommand);
rootCommand.AddCommand(showCommand);
rootCommand.AddCommand(statsCommand);
rootCommand.AddCommand(cleanCommand);
rootCommand.AddCommand(newCommand);
rootCommand.AddCommand(graphCommand);

return await rootCommand.InvokeAsync(args);
