// =============================================================================
// BrowserClickKeyword.cs — кликает по элементу.
// =============================================================================

using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoFlow.Library.Browser;

public sealed class BrowserClickArgs
{
    public string BrowserId { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public int? TimeoutMs { get; set; }
    public bool Force { get; set; } = false;
    public int? DelayMs { get; set; }
}

[Keyword("browser.click", Category = "Browser", Description = "Кликает по элементу.")]
public sealed class BrowserClickKeyword : IKeywordHandler<BrowserClickArgs>
{
    private readonly BrowserManager _browserManager;

    public BrowserClickKeyword(BrowserManager browserManager)
    {
        _browserManager = browserManager;
    }

    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserClickArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Clicking element: {Selector} (browser: {BrowserId})",
            args.Selector, args.BrowserId);

        var page = _browserManager.GetPage(args.BrowserId);

        if (page is null)
        {
            return KeywordResult.Failure($"Browser not found: {args.BrowserId}");
        }

        var options = new PageClickOptions
        {
            Timeout = args.TimeoutMs,
            Force = args.Force,
            Delay = args.DelayMs
        };

        await page.ClickAsync(args.Selector, options).ConfigureAwait(false);

        context.Logger.LogInformation(
            "Clicked element: {Selector}",
            args.Selector);

        return KeywordResult.Success(new
        {
            selector = args.Selector,
            clicked = true
        });
    }
}
