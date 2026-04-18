using System;
using System.Threading.Tasks;
using AutoFlow.Abstractions;

namespace AutoFlow.Library.Browser;

public sealed class BrowserCleanupHook : IWorkflowLifecycleHook
{
    private readonly BrowserManager _browserManager;

    public int Order => 1000;

    public BrowserCleanupHook(BrowserManager browserManager)
    {
        _browserManager = browserManager ?? throw new ArgumentNullException(nameof(browserManager));
    }

    public Task OnWorkflowStartAsync(WorkflowContext ctx)
    {
        return Task.CompletedTask;
    }

    public async Task OnWorkflowEndAsync(WorkflowContext ctx, RunResult result)
    {
        await _browserManager.CloseAllBrowsersAsync().ConfigureAwait(false);
    }

    public async Task OnErrorAsync(WorkflowContext ctx, Exception ex)
    {
        await _browserManager.CloseAllBrowsersAsync().ConfigureAwait(false);
    }

    public Task OnStepStartAsync(StepContext ctx)
    {
        return Task.CompletedTask;
    }

    public Task OnStepEndAsync(StepContext ctx, StepExecutionResult result)
    {
        return Task.CompletedTask;
    }
}
