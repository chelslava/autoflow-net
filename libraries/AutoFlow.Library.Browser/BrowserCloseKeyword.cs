// =============================================================================
// BrowserCloseKeyword.cs — закрывает браузер.
// =============================================================================

using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Library.Browser;

public sealed class BrowserCloseArgs
{
    public string BrowserId { get; set; } = string.Empty;
}

[Keyword("browser.close", Category = "Browser", Description = "Закрывает браузер.")]
public sealed class BrowserCloseKeyword : IKeywordHandler<BrowserCloseArgs>
{
    private readonly BrowserManager _browserManager;

    public BrowserCloseKeyword(BrowserManager browserManager)
    {
        _browserManager = browserManager;
    }

    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserCloseArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Closing browser: {BrowserId}",
            args.BrowserId);

        await _browserManager.CloseBrowserAsync(args.BrowserId).ConfigureAwait(false);

        context.Logger.LogInformation(
            "Browser closed: {BrowserId}",
            args.BrowserId);

        return KeywordResult.Success(new
        {
            browserId = args.BrowserId,
            closed = true
        });
    }
}
