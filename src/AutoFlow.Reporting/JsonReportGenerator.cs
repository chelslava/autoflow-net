using System.Text.Json;
using System.Text.Json.Serialization;
using AutoFlow.Abstractions;

namespace AutoFlow.Reporting;

public sealed class JsonReportGenerator
{
    private readonly JsonSerializerOptions _options;

    public JsonReportGenerator()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition =JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }

    public string Generate(RunResult runResult)
    {
        var report = new JsonReport
        {
            SchemaVersion = "1.0",
            Workflow = new WorkflowInfo
            {
                Name = runResult.WorkflowName,
                Status = runResult.Status.ToString().ToLowerInvariant(),
                StartedAt = runResult.StartedAtUtc.ToString("O"),
                FinishedAt = runResult.FinishedAtUtc.ToString("O"),
                DurationMs = (long)runResult.Duration.TotalMilliseconds
            },
            Summary = new SummaryInfo
            {
                TotalSteps = runResult.Steps.Count,
                PassedSteps = runResult.Steps.Count(s => s.Status == ExecutionStatus.Passed),
                FailedSteps = runResult.Steps.Count(s => s.Status == ExecutionStatus.Failed),
                SkippedSteps = runResult.Steps.Count(s => s.Status == ExecutionStatus.Skipped)
            },
            Steps = runResult.Steps.Select(s => new StepInfo
            {
                Id = s.StepId,
                Keyword = s.KeywordName,
                Status = s.Status.ToString().ToLowerInvariant(),
                StartedAt = s.StartedAtUtc.ToString("O"),
                FinishedAt = s.FinishedAtUtc.ToString("O"),
                DurationMs = (long)s.Duration.TotalMilliseconds,
                Outputs = s.Outputs,
                ErrorMessage = s.ErrorMessage,
                Logs = s.Logs.Count > 0 ? s.Logs : null
            }).ToList()
        };

        return JsonSerializer.Serialize(report, _options);
    }
}

internal sealed class JsonReport
{
    public string SchemaVersion { get; set; } = "1.0";
    public WorkflowInfo Workflow { get; set; } = new();
    public SummaryInfo Summary { get; set; } = new();
    public List<StepInfo> Steps { get; set; } = new();
}

internal sealed class WorkflowInfo
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StartedAt { get; set; } = string.Empty;
    public string FinishedAt { get; set; } = string.Empty;
    public long DurationMs { get; set; }
}

internal sealed class SummaryInfo
{
    public int TotalSteps { get; set; }
    public int PassedSteps { get; set; }
    public int FailedSteps { get; set; }
    public int SkippedSteps { get; set; }
}

internal sealed class StepInfo
{
    public string Id { get; set; } = string.Empty;
    public string Keyword { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StartedAt { get; set; } = string.Empty;
    public string FinishedAt { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public object? Outputs { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string>? Logs { get; set; }
}
