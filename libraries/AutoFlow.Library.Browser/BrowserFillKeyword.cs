// =============================================================================
// BrowserFillKeyword.cs — заполняет поле ввода.
// =============================================================================

using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoFlow.Library.Browser;

public sealed class BrowserFillArgs
{
    public string BrowserId { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int? TimeoutMs { get; set; }
    public bool Clear { get; set; } = true;
    public int? DelayMs { get; set; }
}

[Keyword("browser.fill", Category = "Browser", Description = "Заполняет поле ввода.")]
public sealed class BrowserFillKeyword : IKeywordHandler<BrowserFillArgs>
{
    private readonly BrowserManager _browserManager;

    public BrowserFillKeyword(BrowserManager browserManager)
    {
        _browserManager = browserManager;
    }

    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserFillArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Filling element: {Selector} (browser: {BrowserId})",
            args.Selector, args.BrowserId);

        var page = _browserManager.GetPage(args.BrowserId);

        if (page is null)
        {
            return KeywordResult.Failure($"Browser not found: {args.BrowserId}");
        }

        var options = new PageFillOptions
        {
            Timeout = args.TimeoutMs
        };

        await page.FillAsync(args.Selector, args.Value, options).ConfigureAwait(false);

        context.Logger.LogInformation(
            "Filled element: {Selector}",
            args.Selector);

        return KeywordResult.Success(new
        {
            selector = args.Selector,
            value = args.Value,
            filled = true
        });
    }
}
