using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;

namespace AutoFlow.Library.Assertions;

public sealed class DateTimeArgs
{
    public string? Format { get; set; } = "yyyyMMdd_HHmmss";
}

[Keyword("datetime.now", Category = "Utility", Description = "Returns current date/time as formatted string.")]
public sealed class DateTimeNowKeyword : IKeywordHandler<DateTimeArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        DateTimeArgs args,
        CancellationToken cancellationToken = default)
    {
        var format = args.Format ?? "yyyyMMdd_HHmmss";
        var result = DateTime.Now.ToString(format);

        return Task.FromResult(
            KeywordResult.Success(
                result,
                [$"Current datetime: {result}"]));
    }
}
