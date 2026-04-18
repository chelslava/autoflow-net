// =============================================================================
// HtmlReportGeneratorTests.cs — тесты для генератора HTML отчётов.
// =============================================================================

using AutoFlow.Abstractions;
using AutoFlow.Reporting;
using Xunit;

namespace AutoFlow.Reporting.Tests;

public sealed class HtmlReportGeneratorTests
{
    [Fact]
    public void Generate_WithPassedWorkflow_ReturnsValidHtml()
    {
        var generator = new HtmlReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        runResult.AddStep(CreateTestStep("step1", "log.info", ExecutionStatus.Passed));
        
        var html = generator.Generate(runResult);
        
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<title>AutoFlow Report — TestWorkflow</title>", html);
        Assert.Contains("TestWorkflow", html);
        Assert.Contains("status-passed", html);
        Assert.Contains("step1", html);
        Assert.Contains("log.info", html);
    }

    [Fact]
    public void Generate_WithFailedWorkflow_ShowsFailedStatus()
    {
        var generator = new HtmlReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Failed);
        runResult.AddStep(CreateTestStep("step1", "http.request", ExecutionStatus.Failed, "Connection refused"));
        
        var html = generator.Generate(runResult);
        
        Assert.Contains("status-failed", html);
        Assert.Contains("failed", html);
        Assert.Contains("Connection refused", html);
    }

    [Fact]
    public void Generate_WithMultipleSteps_ShowsAllSteps()
    {
        var generator = new HtmlReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        runResult.AddStep(CreateTestStep("step1", "log.info", ExecutionStatus.Passed));
        runResult.AddStep(CreateTestStep("step2", "files.read", ExecutionStatus.Passed));
        runResult.AddStep(CreateTestStep("step3", "http.request", ExecutionStatus.Failed, "Timeout"));
        
        var html = generator.Generate(runResult);
        
        Assert.Contains("step1", html);
        Assert.Contains("step2", html);
        Assert.Contains("step3", html);
        Assert.Contains("log.info", html);
        Assert.Contains("files.read", html);
        Assert.Contains("http.request", html);
    }

    [Fact]
    public void Generate_WithSkippedSteps_ShowsSkippedStatus()
    {
        var generator = new HtmlReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        runResult.AddStep(CreateTestStep("step1", "log.info", ExecutionStatus.Skipped));
        
        var html = generator.Generate(runResult);
        
        Assert.Contains("skipped", html);
    }

    [Fact]
    public void Generate_WithStepOutputs_IncludesOutputsInReport()
    {
        var generator = new HtmlReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        var step = CreateTestStep("step1", "files.read", ExecutionStatus.Passed);
        step.Outputs = new Dictionary<string, object?> { ["content"] = "test content" };
        runResult.AddStep(step);
        
        var html = generator.Generate(runResult);
        
        Assert.Contains("test content", html);
    }

    [Fact]
    public void Generate_WithStepLogs_IncludesLogsInReport()
    {
        var generator = new HtmlReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        var step = CreateTestStep("step1", "log.info", ExecutionStatus.Passed);
        step.Logs.Add("Starting operation");
        step.Logs.Add("Operation completed");
        runResult.AddStep(step);
        
        var html = generator.Generate(runResult);
        
        Assert.Contains("Starting operation", html);
        Assert.Contains("Operation completed", html);
    }

    [Fact]
    public void Generate_WithSecretMasker_MasksSecretsInReport()
    {
        var masker = new SecretMasker();
        masker.RegisterSecret("secret_value");
        
        var generator = new HtmlReportGenerator(masker);
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        var step = CreateTestStep("step1", "http.request", ExecutionStatus.Passed);
        step.Outputs = new Dictionary<string, object?> { ["token"] = "secret_value" };
        runResult.AddStep(step);
        
        var html = generator.Generate(runResult);
        
        Assert.DoesNotContain("secret_value", html);
        Assert.Contains("***", html);
    }

    [Fact]
    public void Generate_WithSecretMasker_MasksSecretsInErrorMessages()
    {
        var masker = new SecretMasker();
        masker.RegisterSecret("my_secret_password");
        
        var generator = new HtmlReportGenerator(masker);
        var runResult = CreateTestRunResult(ExecutionStatus.Failed);
        runResult.AddStep(CreateTestStep("step1", "http.request", ExecutionStatus.Failed, "Auth failed: my_secret_password"));
        
        var html = generator.Generate(runResult);
        
        Assert.DoesNotContain("my_secret_password", html);
        Assert.Contains("***", html);
    }

    [Fact]
    public void Generate_ContainsSummaryStats()
    {
        var generator = new HtmlReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        runResult.AddStep(CreateTestStep("step1", "log.info", ExecutionStatus.Passed));
        runResult.AddStep(CreateTestStep("step2", "log.info", ExecutionStatus.Passed));
        runResult.AddStep(CreateTestStep("step3", "log.info", ExecutionStatus.Failed, "Error"));
        runResult.AddStep(CreateTestStep("step4", "log.info", ExecutionStatus.Skipped));
        
        var html = generator.Generate(runResult);
        
        Assert.Contains("4", html); // Total steps
        Assert.Contains("Пройдено", html);
        Assert.Contains("С ошибкой", html);
        Assert.Contains("Пропущено", html);
    }

    [Fact]
    public void Generate_ContainsDuration()
    {
        var generator = new HtmlReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        runResult.AddStep(CreateTestStep("step1", "log.info", ExecutionStatus.Passed));
        
        var html = generator.Generate(runResult);
        
        Assert.Contains("ms", html);
    }

    [Fact]
    public void Generate_ContainsStylesAndScripts()
    {
        var generator = new HtmlReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        runResult.AddStep(CreateTestStep("step1", "log.info", ExecutionStatus.Passed));
        
        var html = generator.Generate(runResult);
        
        Assert.Contains("<style>", html);
        Assert.Contains("</style>", html);
        Assert.Contains("<script>", html);
        Assert.Contains("</script>", html);
        Assert.Contains("toggleStep", html);
    }

    private static RunResult CreateTestRunResult(ExecutionStatus status)
    {
        return new RunResult
        {
            WorkflowName = "TestWorkflow",
            Status = status,
            StartedAtUtc = DateTimeOffset.UtcNow.AddSeconds(-5),
            FinishedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static StepExecutionResult CreateTestStep(
        string id,
        string keyword,
        ExecutionStatus status,
        string? errorMessage = null)
    {
        var step = new StepExecutionResult
        {
            StepId = id,
            KeywordName = keyword,
            Status = status,
            StartedAtUtc = DateTimeOffset.UtcNow.AddSeconds(-2),
            FinishedAtUtc = DateTimeOffset.UtcNow.AddSeconds(-1),
            ErrorMessage = errorMessage
        };
        return step;
    }
}
