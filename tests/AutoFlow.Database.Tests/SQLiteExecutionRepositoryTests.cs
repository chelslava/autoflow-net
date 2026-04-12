// =============================================================================
// SQLiteExecutionRepositoryTests.cs — тесты для репозитория SQLite.
// =============================================================================

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using AutoFlow.Database;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AutoFlow.Database.Tests;

public sealed class SQLiteExecutionRepositoryTests : IAsyncLifetime
{
    private readonly string _dbPath;
    private readonly SQLiteExecutionRepository _repository;

    public SQLiteExecutionRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"autoflow_test_{Guid.NewGuid():N}.db");
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger<SQLiteExecutionRepository>();
        _repository = new SQLiteExecutionRepository(_dbPath, logger);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _repository.Dispose();
        
        await Task.Delay(100);
        
        for (var i = 0; i < 5; i++)
        {
            try
            {
                if (File.Exists(_dbPath))
                    File.Delete(_dbPath);
                break;
            }
            catch
            {
                await Task.Delay(200);
            }
        }
    }

    [Fact]
    public async Task SaveAsync_ShouldCreateRecord()
    {
        var result = CreateTestResult("test-workflow", ExecutionStatus.Passed);
        var context = CreateTestContext("run-001");

        var id = await _repository.SaveAsync(result, context);

        Assert.True(id > 0);

        var saved = await _repository.GetByIdAsync(id);
        Assert.NotNull(saved);
        Assert.Equal("test-workflow", saved.WorkflowName);
        Assert.Equal("Passed", saved.Status);
        Assert.Equal("run-001", saved.RunId);
    }

    [Fact]
    public async Task GetByRunIdAsync_ShouldReturnCorrectRecord()
    {
        var result = CreateTestResult("workflow-2", ExecutionStatus.Failed);
        var context = CreateTestContext("run-002");

        await _repository.SaveAsync(result, context);

        var saved = await _repository.GetByRunIdAsync("run-002");

        Assert.NotNull(saved);
        Assert.Equal("workflow-2", saved.WorkflowName);
        Assert.Equal("Failed", saved.Status);
    }

    [Fact]
    public async Task GetListAsync_ShouldReturnAllRecords()
    {
        for (var i = 0; i < 5; i++)
        {
            var result = CreateTestResult($"workflow-{i}", i % 2 == 0 ? ExecutionStatus.Passed : ExecutionStatus.Failed);
            var context = CreateTestContext($"run-{i:D3}");
            await _repository.SaveAsync(result, context);
        }

        var list = await _repository.GetListAsync();

        Assert.Equal(5, list.Count);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterByWorkflowName()
    {
        await _repository.SaveAsync(
            CreateTestResult("alpha-workflow", ExecutionStatus.Passed),
            CreateTestContext("run-001"));
        await _repository.SaveAsync(
            CreateTestResult("beta-workflow", ExecutionStatus.Passed),
            CreateTestContext("run-002"));
        await _repository.SaveAsync(
            CreateTestResult("alpha-workflow", ExecutionStatus.Failed),
            CreateTestContext("run-003"));

        var list = await _repository.GetListAsync(workflowName: "alpha-workflow");

        Assert.Equal(2, list.Count);
        Assert.All(list, r => Assert.Equal("alpha-workflow", r.WorkflowName));
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterByStatus()
    {
        await _repository.SaveAsync(
            CreateTestResult("workflow", ExecutionStatus.Passed),
            CreateTestContext("run-001"));
        await _repository.SaveAsync(
            CreateTestResult("workflow", ExecutionStatus.Failed),
            CreateTestContext("run-002"));
        await _repository.SaveAsync(
            CreateTestResult("workflow", ExecutionStatus.Passed),
            CreateTestContext("run-003"));

        var list = await _repository.GetListAsync(status: "Failed");

        Assert.Single(list);
        Assert.Equal("Failed", list[0].Status);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_ShouldRemoveOldRecords()
    {
        var oldResult = new RunResult
        {
            WorkflowName = "old-workflow",
            Status = ExecutionStatus.Passed,
            StartedAtUtc = DateTimeOffset.UtcNow.AddDays(-10),
            FinishedAtUtc = DateTimeOffset.UtcNow.AddDays(-10).AddSeconds(1)
        };
        await _repository.SaveAsync(oldResult, CreateTestContext("run-old"));

        var newResult = CreateTestResult("new-workflow", ExecutionStatus.Passed);
        await _repository.SaveAsync(newResult, CreateTestContext("run-new"));

        var deleted = await _repository.DeleteOlderThanAsync(5);

        Assert.Equal(1, deleted);

        var list = await _repository.GetListAsync();
        Assert.Single(list);
        Assert.Equal("new-workflow", list[0].WorkflowName);
    }

    [Fact]
    public async Task DeleteByRunIdAsync_ShouldRemoveCorrectRecord()
    {
        await _repository.SaveAsync(
            CreateTestResult("workflow-1", ExecutionStatus.Passed),
            CreateTestContext("run-001"));
        await _repository.SaveAsync(
            CreateTestResult("workflow-2", ExecutionStatus.Passed),
            CreateTestContext("run-002"));

        var deleted = await _repository.DeleteByRunIdAsync("run-001");

        Assert.True(deleted);

        var list = await _repository.GetListAsync();
        Assert.Single(list);
        Assert.Equal("run-002", list[0].RunId);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCorrectStats()
    {
        for (var i = 0; i < 10; i++)
        {
            var result = CreateTestResult("stats-workflow", i < 7 ? ExecutionStatus.Passed : ExecutionStatus.Failed);
            var context = CreateTestContext($"run-stats-{i:D2}");
            await _repository.SaveAsync(result, context);
        }

        var stats = await _repository.GetStatisticsAsync("stats-workflow");

        Assert.Equal(10, stats.TotalRuns);
        Assert.Equal(7, stats.PassedRuns);
        Assert.Equal(3, stats.FailedRuns);
        Assert.Equal(70.0, stats.SuccessRate, 1);
        Assert.True(stats.AverageDurationMs > 0);
    }

    [Fact]
    public async Task SaveAsync_ShouldStoreStepsJson()
    {
        var result = CreateTestResult("steps-workflow", ExecutionStatus.Passed);
        result.Steps.Add(new StepExecutionResult
        {
            StepId = "step-1",
            KeywordName = "log.info",
            Status = ExecutionStatus.Passed,
            StartedAtUtc = DateTimeOffset.UtcNow,
            FinishedAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(50)
        });

        await _repository.SaveAsync(result, CreateTestContext("run-steps"));

        var saved = await _repository.GetByRunIdAsync("run-steps");
        Assert.NotNull(saved);
        Assert.NotNull(saved.StepsJson);
        Assert.Contains("step-1", saved.StepsJson);
    }

    private static RunResult CreateTestResult(string workflowName, ExecutionStatus status)
    {
        return new RunResult
        {
            WorkflowName = workflowName,
            Status = status,
            StartedAtUtc = DateTimeOffset.UtcNow,
            FinishedAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(100)
        };
    }

    private static WorkflowContext CreateTestContext(string runId)
    {
        return new WorkflowContext
        {
            RunId = runId,
            WorkflowName = "test"
        };
    }
}
