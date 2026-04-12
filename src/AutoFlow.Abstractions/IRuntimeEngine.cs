// Этот код нужен для запуска workflow-документа через runtime-движок.
using System.Threading;
using System.Threading.Tasks;

namespace AutoFlow.Abstractions;

public interface IRuntimeEngine
{
    Task<RunResult> ExecuteAsync(
        WorkflowDocument document,
        RuntimeLaunchOptions options,
        CancellationToken cancellationToken = default);
}
