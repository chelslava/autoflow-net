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
    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserCloseArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Closing browser: {BrowserId}",
            args.BrowserId);

        var page = BrowserOpenKeyword.GetPage(args.BrowserId);
        var browser = BrowserOpenKeyword.GetBrowser(args.BrowserId);

        if (page is not null)
        {
            await page.CloseAsync().ConfigureAwait(false);
        }

        if (browser is not null)
        {
            await browser.CloseAsync().ConfigureAwait(false);
            BrowserOpenKeyword.RemoveBrowser(args.BrowserId);
        }

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
