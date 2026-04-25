

using AutoFlow.Abstractions;
using AutoFlow.Reporting;
using Xunit;

namespace AutoFlow.Reporting.Tests;

public sealed class JsonReportGeneratorTests
{
    [Fact]
    public void Generate_WithPassedWorkflow_ReturnsValidJson()
    {
        var generator = new JsonReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        runResult.AddStep(CreateTestStep("step1", "log.info", ExecutionStatus.Passed));

        var json = generator.Generate(runResult);

        Assert.Contains("\"schemaVersion\": \"1.0\"", json);
        Assert.Contains("\"workflow\"", json);
        Assert.Contains("\"summary\"", json);
        Assert.Contains("\"steps\"", json);
    }

    [Fact]
    public void Generate_WithPassedWorkflow_SetsCorrectStatus()
    {
        var generator = new JsonReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);

        var json = generator.Generate(runResult);

        Assert.Contains("\"status\": \"passed\"", json);
    }

    [Fact]
    public void Generate_WithFailedWorkflow_SetsFailedStatus()
    {
        var generator = new JsonReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Failed);

        var json = generator.Generate(runResult);

        Assert.Contains("\"status\": \"failed\"", json);
    }

    [Fact]
    public void Generate_WithMultipleSteps_SetsCorrectSummary()
    {
        var generator = new JsonReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        runResult.AddStep(CreateTestStep("step1", "log.info", ExecutionStatus.Passed));
        runResult.AddStep(CreateTestStep("step2", "log.info", ExecutionStatus.Passed));
        runResult.AddStep(CreateTestStep("step3", "log.info", ExecutionStatus.Failed, "Error"));
        runResult.AddStep(CreateTestStep("step4", "log.info", ExecutionStatus.Skipped));

        var json = generator.Generate(runResult);

        Assert.Contains("\"totalSteps\": 4", json);
        Assert.Contains("\"passedSteps\": 2", json);
        Assert.Contains("\"failedSteps\": 1", json);
        Assert.Contains("\"skippedSteps\": 1", json);
    }

    [Fact]
    public void Generate_WithStepOutputs_IncludesOutputsInJson()
    {
        var generator = new JsonReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        var step = CreateTestStep("step1", "files.read", ExecutionStatus.Passed);
        step.Outputs = new Dictionary<string, object?> { ["content"] = "test content" };
        runResult.AddStep(step);

        var json = generator.Generate(runResult);

        Assert.Contains("\"outputs\"", json);
        Assert.Contains("\"content\": \"test content\"", json);
    }

    [Fact]
    public void Generate_WithStepLogs_IncludesLogsInJson()
    {
        var generator = new JsonReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        var step = CreateTestStep("step1", "log.info", ExecutionStatus.Passed);
        step.Logs.Add("Starting operation");
        step.Logs.Add("Operation completed");
        runResult.AddStep(step);

        var json = generator.Generate(runResult);

        Assert.Contains("\"logs\"", json);
        Assert.Contains("\"Starting operation\"", json);
        Assert.Contains("\"Operation completed\"", json);
    }

    [Fact]
    public void Generate_WithStepErrorMessage_IncludesErrorMessage()
    {
        var generator = new JsonReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Failed);
        runResult.AddStep(CreateTestStep("step1", "http.request", ExecutionStatus.Failed, "Connection refused"));

        var json = generator.Generate(runResult);

        Assert.Contains("\"errorMessage\": \"Connection refused\"", json);
    }

    [Fact]
    public void Generate_WithSecretMasker_MasksSecretsInOutputs()
    {
        var masker = new SecretMasker();
        masker.RegisterSecret("secret_value");

        var generator = new JsonReportGenerator(masker);
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        var step = CreateTestStep("step1", "http.request", ExecutionStatus.Passed);
        step.Outputs = new Dictionary<string, object?> { ["token"] = "secret_value" };
        runResult.AddStep(step);

        var json = generator.Generate(runResult);

        Assert.DoesNotContain("secret_value", json);
        Assert.Contains("\"token\": \"***\"", json);
    }

    [Fact]
    public void Generate_WithSecretMasker_MasksSecretsInErrorMessages()
    {
        var masker = new SecretMasker();
        masker.RegisterSecret("my_secret_password");

        var generator = new JsonReportGenerator(masker);
        var runResult = CreateTestRunResult(ExecutionStatus.Failed);
        runResult.AddStep(CreateTestStep("step1", "http.request", ExecutionStatus.Failed, "Auth failed: my_secret_password"));

        var json = generator.Generate(runResult);

        Assert.DoesNotContain("my_secret_password", json);
        Assert.Contains("\"errorMessage\": \"Auth failed: ***\"", json);
    }

    [Fact]
    public void Generate_WithSecretMasker_MasksSecretsInLogs()
    {
        var masker = new SecretMasker();
        masker.RegisterSecret("my_secret_log");

        var generator = new JsonReportGenerator(masker);
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        var step = CreateTestStep("step1", "log.info", ExecutionStatus.Passed);
        step.Logs.Add("Starting with my_secret_log");
        runResult.AddStep(step);

        var json = generator.Generate(runResult);

        Assert.DoesNotContain("my_secret_log", json);
        Assert.Contains("\"Starting with ***\"", json);
    }

    [Fact]
    public void Generate_WithCamelCaseNaming_PropertiesAreCamelCase()
    {
        var generator = new JsonReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        runResult.AddStep(CreateTestStep("step1", "log.info", ExecutionStatus.Passed));

        var json = generator.Generate(runResult);

        Assert.Contains("\"schemaVersion\"", json);
        Assert.Contains("\"name\"", json);
        Assert.Contains("\"id\"", json);
        Assert.Contains("\"keyword\"", json);
    }

    [Fact]
    public void Generate_IncludesDurationInMilliseconds()
    {
        var generator = new JsonReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        runResult.AddStep(CreateTestStep("step1", "log.info", ExecutionStatus.Passed));

        var json = generator.Generate(runResult);

        Assert.Contains("\"durationMs\"", json);
    }

    [Fact]
    public void Generate_IncludesTimestampsInIsoFormat()
    {
        var generator = new JsonReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);

        var json = generator.Generate(runResult);

        Assert.Contains("\"startedAt\"", json);
        Assert.Contains("\"finishedAt\"", json);
    }

    [Fact]
    public void Generate_NoMasker_DoesNotMaskSecrets()
    {
        var generator = new JsonReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);
        var step = CreateTestStep("step1", "http.request", ExecutionStatus.Passed);
        step.Outputs = new Dictionary<string, object?> { ["token"] = "secret_value" };
        runResult.AddStep(step);

        var json = generator.Generate(runResult);

        Assert.Contains("\"token\": \"secret_value\"", json);
    }

    [Fact]
    public void Generate_WithEmptySteps_SetsZeroSummary()
    {
        var generator = new JsonReportGenerator();
        var runResult = CreateTestRunResult(ExecutionStatus.Passed);

        var json = generator.Generate(runResult);

        Assert.Contains("\"totalSteps\": 0", json);
        Assert.Contains("\"passedSteps\": 0", json);
        Assert.Contains("\"failedSteps\": 0", json);
        Assert.Contains("\"skippedSteps\": 0", json);
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
