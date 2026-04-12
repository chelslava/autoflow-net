// =============================================================================
// BrowserGotoKeyword.cs — навигирует на указанный URL.
// =============================================================================

using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoFlow.Library.Browser;

public sealed class BrowserGotoArgs
{
    public string BrowserId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int? TimeoutMs { get; set; }
    public bool WaitUntilLoad { get; set; } = true;
}

[Keyword("browser.goto", Category = "Browser", Description = "Навигирует на указанный URL.")]
public sealed class BrowserGotoKeyword : IKeywordHandler<BrowserGotoArgs>
{
    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserGotoArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Navigating to: {Url} (browser: {BrowserId})",
            args.Url, args.BrowserId);

        var page = BrowserOpenKeyword.GetPage(args.BrowserId);

        if (page is null)
        {
            return KeywordResult.Failure($"Browser not found: {args.BrowserId}");
        }

        var options = new PageGotoOptions
        {
            Timeout = args.TimeoutMs,
            WaitUntil = args.WaitUntilLoad
                ? WaitUntilState.Load
                : WaitUntilState.DOMContentLoaded
        };

        var response = await page.GotoAsync(args.Url, options).ConfigureAwait(false);

        var title = await page.TitleAsync().ConfigureAwait(false);
        var url = page.Url;

        context.Logger.LogInformation(
            "Navigated to: {Url} (title: {Title})",
            url, title);

        return KeywordResult.Success(new
        {
            url,
            title,
            statusCode = response?.Status,
            statusText = response?.StatusText
        });
    }
}
