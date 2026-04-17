// =============================================================================
// BrowserEvaluateKeyword.cs — выполняет JavaScript в браузере.
// =============================================================================

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Library.Browser;

public sealed class BrowserEvaluateArgs
{
    public string BrowserId { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public object? Arg { get; set; }
}

[Keyword("browser.evaluate", Category = "Browser", Description = "Выполняет JavaScript в браузере.")]
public sealed class BrowserEvaluateKeyword : IKeywordHandler<BrowserEvaluateArgs>
{
    private readonly BrowserManager _browserManager;

    public BrowserEvaluateKeyword(BrowserManager browserManager)
    {
        _browserManager = browserManager;
    }

    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserEvaluateArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Evaluating script (browser: {BrowserId})",
            args.BrowserId);

        var page = _browserManager.GetPage(args.BrowserId);

        if (page is null)
        {
            return KeywordResult.Failure($"Browser not found: {args.BrowserId}");
        }

        var result = await page.EvaluateAsync(args.Script, args.Arg).ConfigureAwait(false);

        context.Logger.LogInformation(
            "Script executed successfully");

        return KeywordResult.Success(new
        {
            result
        });
    }
}
