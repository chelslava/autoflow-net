// =============================================================================
// BrowserGetTextKeyword.cs — получает текст элемента.
// =============================================================================

using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoFlow.Library.Browser;

public sealed class BrowserGetTextArgs
{
    public string BrowserId { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public int? TimeoutMs { get; set; }
}

[Keyword("browser.get_text", Category = "Browser", Description = "Получает текст элемента.")]
public sealed class BrowserGetTextKeyword : IKeywordHandler<BrowserGetTextArgs>
{
    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserGetTextArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Getting text from element: {Selector} (browser: {BrowserId})",
            args.Selector, args.BrowserId);

        var page = BrowserOpenKeyword.GetPage(args.BrowserId);

        if (page is null)
        {
            return KeywordResult.Failure($"Browser not found: {args.BrowserId}");
        }

        var locator = page.Locator(args.Selector);
        var text = await locator.TextContentAsync(new LocatorTextContentOptions
        {
            Timeout = args.TimeoutMs
        }).ConfigureAwait(false);

        context.Logger.LogInformation(
            "Got text: {Text}",
            text);

        return KeywordResult.Success(new
        {
            selector = args.Selector,
            text = text ?? string.Empty
        });
    }
}
