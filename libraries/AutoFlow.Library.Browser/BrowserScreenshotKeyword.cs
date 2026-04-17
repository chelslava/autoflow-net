// =============================================================================
// BrowserScreenshotKeyword.cs — делает скриншот страницы.
// =============================================================================

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoFlow.Library.Browser;

public sealed class BrowserScreenshotArgs
{
    public string BrowserId { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool FullPage { get; set; } = false;
    public string? Selector { get; set; }
}

[Keyword("browser.screenshot", Category = "Browser", Description = "Делает скриншот страницы.")]
public sealed class BrowserScreenshotKeyword : IKeywordHandler<BrowserScreenshotArgs>
{
    private readonly BrowserManager _browserManager;

    public BrowserScreenshotKeyword(BrowserManager browserManager)
    {
        _browserManager = browserManager;
    }

    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserScreenshotArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Taking screenshot: {Path} (browser: {BrowserId})",
            args.Path, args.BrowserId);

        var page = _browserManager.GetPage(args.BrowserId);

        if (page is null)
        {
            return KeywordResult.Failure($"Browser not found: {args.BrowserId}");
        }

        var directory = Path.GetDirectoryName(args.Path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        byte[] screenshotBytes;

        if (!string.IsNullOrEmpty(args.Selector))
        {
            var element = await page.QuerySelectorAsync(args.Selector).ConfigureAwait(false);
            if (element is null)
            {
                return KeywordResult.Failure($"Element not found: {args.Selector}");
            }
            screenshotBytes = await element.ScreenshotAsync(new ElementHandleScreenshotOptions
            {
                Path = args.Path
            }).ConfigureAwait(false);
        }
        else
        {
            screenshotBytes = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = args.Path,
                FullPage = args.FullPage
            }).ConfigureAwait(false);
        }

        context.Logger.LogInformation(
            "Screenshot saved: {Path} ({Size} bytes)",
            args.Path, screenshotBytes.Length);

        return KeywordResult.Success(new
        {
            path = args.Path,
            size = screenshotBytes.Length,
            fullPage = args.FullPage,
            selector = args.Selector
        });
    }
}
