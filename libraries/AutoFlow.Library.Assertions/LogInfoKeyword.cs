// Этот код нужен для простого logging keyword, с которого удобно начинать MVP.
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Library.Assertions;

[Keyword("log.info", Category = "Logging", Description = "Записывает информационное сообщение в лог.")]
public sealed class LogInfoKeyword : IKeywordHandler<LogInfoArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        LogInfoArgs args,
        CancellationToken cancellationToken = default)
    {
        var message = args.Message ?? string.Empty;

        context.Logger.LogInformation("Шаг {StepId}: {Message}", context.StepId, message);

        return Task.FromResult(
            KeywordResult.Success(
                new { message },
                [$"INFO: {message}"]));
    }
}
