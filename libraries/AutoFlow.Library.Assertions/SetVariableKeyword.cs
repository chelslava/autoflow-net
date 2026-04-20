using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;

namespace AutoFlow.Library.Assertions;

public sealed class SetVariableArgs
{
    public string? Value { get; set; }
}

[Keyword("variables.set", Category = "Variables", Description = "Sets a variable value and returns it for save_as.")]
public sealed class SetVariableKeyword : IKeywordHandler<SetVariableArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        SetVariableArgs args,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            KeywordResult.Success(
                args.Value,
                [$"Variable set: {args.Value}"]));
    }
}
