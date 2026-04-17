// =============================================================================
// BrowserHoverKeyword.cs — наводит курсор на элемент.
// =============================================================================

using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoFlow.Library.Browser;

public sealed class BrowserHoverArgs
{
    public string BrowserId { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public int? TimeoutMs { get; set; }
    public bool Force { get; set; } = false;
}

[Keyword("browser.hover", Category = "Browser", Description = "Наводит курсор на элемент.")]
public sealed class BrowserHoverKeyword : IKeywordHandler<BrowserHoverArgs>
{
    private readonly BrowserManager _browserManager;

    public BrowserHoverKeyword(BrowserManager browserManager)
    {
        _browserManager = browserManager;
    }

    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserHoverArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Hovering element: {Selector} (browser: {BrowserId})",
            args.Selector, args.BrowserId);

        var page = _browserManager.GetPage(args.BrowserId);

        if (page is null)
        {
            return KeywordResult.Failure($"Browser not found: {args.BrowserId}");
        }

        await page.HoverAsync(args.Selector, new PageHoverOptions
        {
            Timeout = args.TimeoutMs,
            Force = args.Force
        }).ConfigureAwait(false);

        context.Logger.LogInformation(
            "Hovered element: {Selector}",
            args.Selector);

        return KeywordResult.Success(new
        {
            selector = args.Selector,
            hovered = true
        });
    }
}
