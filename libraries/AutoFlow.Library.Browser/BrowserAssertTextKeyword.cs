// =============================================================================
// BrowserAssertTextKeyword.cs — проверяет текст на странице.
// =============================================================================

using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoFlow.Library.Browser;

public sealed class BrowserAssertTextArgs
{
    public string BrowserId { get; set; } = string.Empty;
    public string? Selector { get; set; }
    public string Expected { get; set; } = string.Empty;
    public bool Contains { get; set; } = true;
    public int? TimeoutMs { get; set; }
}

[Keyword("browser.assert_text", Category = "Browser", Description = "Проверяет текст на странице.")]
public sealed class BrowserAssertTextKeyword : IKeywordHandler<BrowserAssertTextArgs>
{
    private readonly BrowserManager _browserManager;

    public BrowserAssertTextKeyword(BrowserManager browserManager)
    {
        _browserManager = browserManager;
    }

    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserAssertTextArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Asserting text: {Expected} (browser: {BrowserId})",
            args.Expected, args.BrowserId);

        var page = _browserManager.GetPage(args.BrowserId);

        if (page is null)
        {
            return KeywordResult.Failure($"Browser not found: {args.BrowserId}");
        }

        string? actualText;

        if (!string.IsNullOrEmpty(args.Selector))
        {
            var locator = page.Locator(args.Selector);
            actualText = await locator.TextContentAsync(new LocatorTextContentOptions
            {
                Timeout = args.TimeoutMs
            }).ConfigureAwait(false);
        }
        else
        {
            actualText = await page.TextContentAsync("body").ConfigureAwait(false);
        }

        var passed = args.Contains
            ? actualText?.Contains(args.Expected) ?? false
            : actualText?.Trim() == args.Expected.Trim();

        if (!passed)
        {
            var message = args.Contains
                ? $"Expected text to contain '{args.Expected}', but got '{actualText}'"
                : $"Expected text '{args.Expected}', but got '{actualText}'";
            return KeywordResult.Failure(message);
        }

        context.Logger.LogInformation(
            "Text assertion passed: {Expected}",
            args.Expected);

        return KeywordResult.Success(new
        {
            selector = args.Selector ?? "body",
            expected = args.Expected,
            actual = actualText,
            contains = args.Contains,
            passed = true
        });
    }
}
