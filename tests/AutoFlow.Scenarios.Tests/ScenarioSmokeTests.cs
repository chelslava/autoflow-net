using System.Text;
using AutoFlow.Abstractions;
using AutoFlow.Validation;
using Xunit;

namespace AutoFlow.Scenarios.Tests;

public sealed class ScenarioSmokeTests
{
    public static TheoryData<string> ValidatableExampleEntryPoints =>
    [
        "examples/flow.yaml",
        "examples/advanced_flow.yaml",
        "examples/advanced_features.yaml",
        "examples/file_roundtrip.yaml",
        "examples/http_json_report.yaml",
        "examples/excel_summary.yaml",
        "examples/imports/main.yaml",
        "examples/imports_report.yaml",
        "examples/parallel_fetch_report.yaml",
        "examples/report_cli_demo.yaml"
    ];

    public static TheoryData<string> LocalScenarioExamples =>
    [
        "examples/flow.yaml",
        "examples/advanced_flow.yaml",
        "examples/file_roundtrip.yaml",
        "examples/imports/main.yaml",
        "examples/imports_report.yaml",
        "examples/report_cli_demo.yaml"
    ];

    public static TheoryData<string> ExternalScenarioExamples =>
    [
        "examples/advanced_features.yaml",
        "examples/http_json_report.yaml",
        "examples/parallel_fetch_report.yaml"
    ];

    [Theory]
    [MemberData(nameof(ValidatableExampleEntryPoints))]
    [Trait("Category", "Scenario")]
    public async Task Validate_ExampleEntryPoints_Pass(string relativePath)
    {
        await ScenarioTestHarness.RunInRepoAsync(() =>
        {
            using var host = ScenarioTestHost.Create();
            var fullPath = ScenarioTestHarness.GetPath(relativePath.Split('/', '\\'));
            var document = host.Loader.LoadFromFile(fullPath);
            var validation = new WorkflowValidator(host.Registry).Validate(document);

            Assert.True(validation.IsValid, FormatErrors(relativePath, validation.Errors));
            return Task.CompletedTask;
        });
    }

    [Theory]
    [MemberData(nameof(LocalScenarioExamples))]
    [Trait("Category", "Scenario")]
    public async Task Run_LocalScenarioExamples_Pass(string relativePath)
    {
        await ScenarioTestHarness.RunInRepoAsync(async () =>
        {
            using var host = ScenarioTestHost.Create();
            var result = await ExecuteWorkflowAsync(host, relativePath);

            Assert.True(result.Status == ExecutionStatus.Passed, FormatRunFailure(relativePath, result));
        });
    }

    [Theory]
    [MemberData(nameof(ExternalScenarioExamples))]
    [Trait("Category", "ExternalScenario")]
    public async Task Run_ExternalScenarioExamples_Pass(string relativePath)
    {
        await ScenarioTestHarness.RunInRepoAsync(async () =>
        {
            using var host = ScenarioTestHost.Create();
            var result = await ExecuteWorkflowAsync(host, relativePath);

            Assert.True(result.Status == ExecutionStatus.Passed, FormatRunFailure(relativePath, result));
        });
    }

    [Fact]
    [Trait("Category", "Scenario")]
    public async Task Run_ReportCliDemo_GeneratesJsonAndHtmlReports()
    {
        await ScenarioTestHarness.RunInRepoAsync(async () =>
        {
            using var host = ScenarioTestHost.Create();
            var result = await ExecuteWorkflowAsync(host, "examples/report_cli_demo.yaml");

            Assert.Equal(ExecutionStatus.Passed, result.Status);

            var reportsDirectory = ScenarioTestHarness.GetPath("reports", "scenario-tests");
            Directory.CreateDirectory(reportsDirectory);

            var jsonPath = Path.Join(reportsDirectory, $"report-{Guid.NewGuid():N}.json");
            var htmlPath = Path.Join(reportsDirectory, $"report-{Guid.NewGuid():N}.html");

            await File.WriteAllTextAsync(jsonPath, host.JsonReportGenerator.Generate(result));
            await File.WriteAllTextAsync(htmlPath, host.HtmlReportGenerator.Generate(result));

            Assert.True(File.Exists(jsonPath));
            Assert.True(File.Exists(htmlPath));
            Assert.Contains("report_cli_demo", await File.ReadAllTextAsync(jsonPath));
            Assert.Contains("AutoFlow Report", await File.ReadAllTextAsync(htmlPath));
        });
    }

    [Fact]
    [Trait("Category", "Scenario")]
    public async Task Run_FileAndImportExamples_CreateExpectedArtifacts()
    {
        await ScenarioTestHarness.RunInRepoAsync(async () =>
        {
            using var host = ScenarioTestHost.Create();

            var fileRoundtripResult = await ExecuteWorkflowAsync(host, "examples/file_roundtrip.yaml");
            var importsReportResult = await ExecuteWorkflowAsync(host, "examples/imports_report.yaml");

            Assert.Equal(ExecutionStatus.Passed, fileRoundtripResult.Status);
            Assert.Equal(ExecutionStatus.Passed, importsReportResult.Status);

            var fileRoundtripReportPath = ScenarioTestHarness.GetPath("output", "file_roundtrip", "report.txt");
            var importsReportPath = ScenarioTestHarness.GetPath("output", "imports_report", "summary.txt");

            Assert.True(File.Exists(fileRoundtripReportPath));
            Assert.True(File.Exists(importsReportPath));
            Assert.Contains("File roundtrip example", await File.ReadAllTextAsync(fileRoundtripReportPath));
            Assert.Contains("Imports example", await File.ReadAllTextAsync(importsReportPath));
        });
    }

    [Fact]
    [Trait("Category", "ExternalScenario")]
    public async Task Run_ParallelFetchExample_CreatesSavedArtifacts()
    {
        await ScenarioTestHarness.RunInRepoAsync(async () =>
        {
            using var host = ScenarioTestHost.Create();
            var result = await ExecuteWorkflowAsync(host, "examples/parallel_fetch_report.yaml");

            Assert.True(result.Status == ExecutionStatus.Passed, FormatRunFailure("examples/parallel_fetch_report.yaml", result));
            Assert.True(File.Exists(ScenarioTestHarness.GetPath("output", "parallel_fetch_report", "summary.txt")));
            Assert.True(File.Exists(ScenarioTestHarness.GetPath("output", "parallel_fetch_report", "post.json")));
            Assert.True(File.Exists(ScenarioTestHarness.GetPath("output", "parallel_fetch_report", "todo.json")));
            Assert.True(File.Exists(ScenarioTestHarness.GetPath("output", "parallel_fetch_report", "user.json")));
        });
    }

    private static async Task<RunResult> ExecuteWorkflowAsync(ScenarioTestHost host, string relativePath)
    {
        var fullPath = ScenarioTestHarness.GetPath(relativePath.Split('/', '\\'));
        var document = host.Loader.LoadFromFile(fullPath);
        var validation = new WorkflowValidator(host.Registry).Validate(document);

        Assert.True(validation.IsValid, FormatErrors(relativePath, validation.Errors));
        return await host.Runtime.ExecuteAsync(document, new RuntimeLaunchOptions());
    }

    private static string FormatErrors(string workflowPath, IEnumerable<ValidationError> errors)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Workflow validation failed for {workflowPath}:");

        foreach (var error in errors)
        {
            builder.AppendLine($"- [{error.Code}] {error.Message} ({error.Location})");
        }

        return builder.ToString();
    }

    private static string FormatRunFailure(string workflowPath, RunResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Workflow execution failed for {workflowPath}. Final status: {result.Status}");

        foreach (var failedStep in result.Steps.Where(step => step.Status == ExecutionStatus.Failed))
        {
            builder.AppendLine($"- Step {failedStep.StepId} ({failedStep.KeywordName}): {failedStep.ErrorMessage}");
        }

        return builder.ToString();
    }
}
