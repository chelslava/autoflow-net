// Этот код нужен для определения базового контракта обработчика keyword.
using System.Threading;
using System.Threading.Tasks;

namespace AutoFlow.Abstractions;

public interface IKeywordHandler<in TArgs>
{
    Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        TArgs args,
        CancellationToken cancellationToken = default);
}
