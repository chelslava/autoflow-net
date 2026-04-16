using System.Threading;
using System.Threading.Tasks;

namespace AutoFlow.Abstractions;

/// <summary>
/// Contract for keyword handlers. Implement this interface to create custom workflow keywords.
/// </summary>
/// <typeparam name="TArgs">The argument type for the keyword.</typeparam>
public interface IKeywordHandler<in TArgs>
{
    /// <summary>
    /// Executes the keyword with the provided arguments.
    /// </summary>
    /// <param name="context">The keyword execution context.</param>
    /// <param name="args">The keyword arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the keyword execution.</returns>
    Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        TArgs args,
        CancellationToken cancellationToken = default);
}
