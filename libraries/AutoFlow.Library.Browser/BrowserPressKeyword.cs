// =============================================================================
// BrowserPressKeyword.cs — нажимает клавиши.
// =============================================================================

using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoFlow.Library.Browser;

public sealed class BrowserPressArgs
{
    public string BrowserId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Selector { get; set; }
    public int? DelayMs { get; set; }
}

[Keyword("browser.press", Category = "Browser", Description = "Нажимает клавиши.")]
public sealed class BrowserPressKeyword : IKeywordHandler<BrowserPressArgs>
{
    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserPressArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Pressing key: {Key} (browser: {BrowserId})",
            args.Key, args.BrowserId);

        var page = BrowserOpenKeyword.GetPage(args.BrowserId);

        if (page is null)
        {
            return KeywordResult.Failure($"Browser not found: {args.BrowserId}");
        }

        if (!string.IsNullOrEmpty(args.Selector))
        {
            await page.PressAsync(args.Selector, args.Key, new PagePressOptions
            {
                Delay = args.DelayMs
            }).ConfigureAwait(false);
        }
        else
        {
            await page.Keyboard.PressAsync(args.Key, new KeyboardPressOptions
            {
                Delay = args.DelayMs
            }).ConfigureAwait(false);
        }

        context.Logger.LogInformation(
            "Pressed key: {Key}",
            args.Key);

        return KeywordResult.Success(new
        {
            key = args.Key,
            selector = args.Selector,
            pressed = true
        });
    }
}
