using System;
using System.Threading.Tasks;
using AutoFlow.Abstractions;

namespace AutoFlow.Runtime.Hooks;

public sealed class ProgressHook : IWorkflowLifecycleHook
{
    private int _totalSteps;
    private int _completedSteps;

    public int Order => 100;

    public void SetTotalSteps(int count)
    {
        _totalSteps = count;
        _completedSteps = 0;
    }

    public Task OnWorkflowStartAsync(WorkflowContext ctx)
    {
        Console.WriteLine();
        return Task.CompletedTask;
    }

    public Task OnWorkflowEndAsync(WorkflowContext ctx, RunResult result)
    {
        Console.WriteLine();
        return Task.CompletedTask;
    }

    public Task OnStepStartAsync(StepContext ctx)
    {
        return Task.CompletedTask;
    }

    public Task OnStepEndAsync(StepContext ctx, StepExecutionResult result)
    {
        _completedSteps++;
        
        var icon = result.Status == ExecutionStatus.Passed ? "✓" : "✗";
        var progress = _totalSteps > 0 
            ? $"[{_completedSteps}/{_totalSteps}]" 
            : $"[{_completedSteps}]";
        
        var duration = (long)result.Duration.TotalMilliseconds;
        Console.WriteLine($"{progress} {icon} {ctx.StepId}: {ctx.KeywordName} ({duration}ms)");
        
        if (result.ErrorMessage is not null)
        {
            Console.WriteLine($"      └─ Error: {Truncate(result.ErrorMessage, 100)}");
        }
        
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(WorkflowContext ctx, Exception ex)
    {
        Console.WriteLine($"✗ Error: {ex.Message}");
        return Task.CompletedTask;
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..maxLength] + "...";
    }
}
