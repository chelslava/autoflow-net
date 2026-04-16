using System.Threading;
using System.Threading.Tasks;

namespace AutoFlow.Abstractions;

/// <summary>
/// Contract for the workflow execution engine.
/// </summary>
public interface IRuntimeEngine
{
    /// <summary>
    /// Executes a workflow document with the specified options.
    /// </summary>
    /// <param name="document">The workflow document to execute.</param>
    /// <param name="options">Runtime launch options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the workflow execution.</returns>
    Task<RunResult> ExecuteAsync(
        WorkflowDocument document,
        RuntimeLaunchOptions options,
        CancellationToken cancellationToken = default);
}
