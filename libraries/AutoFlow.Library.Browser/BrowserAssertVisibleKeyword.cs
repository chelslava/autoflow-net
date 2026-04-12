// =============================================================================
// BrowserAssertVisibleKeyword.cs — проверяет видимость элемента.
// =============================================================================

using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Library.Browser;

public sealed class BrowserAssertVisibleArgs
{
    public string BrowserId { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public int? TimeoutMs { get; set; }
}

[Keyword("browser.assert_visible", Category = "Browser", Description = "Проверяет видимость элемента.")]
public sealed class BrowserAssertVisibleKeyword : IKeywordHandler<BrowserAssertVisibleArgs>
{
    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserAssertVisibleArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Asserting visibility: {Selector} (browser: {BrowserId})",
            args.Selector, args.BrowserId);

        var page = BrowserOpenKeyword.GetPage(args.BrowserId);

        if (page is null)
        {
            return KeywordResult.Failure($"Browser not found: {args.BrowserId}");
        }

        var locator = page.Locator(args.Selector);
        var isVisible = await locator.IsVisibleAsync().ConfigureAwait(false);

        if (!isVisible)
        {
            return KeywordResult.Failure($"Element is not visible: {args.Selector}");
        }

        context.Logger.LogInformation(
            "Element is visible: {Selector}",
            args.Selector);

        return KeywordResult.Success(new
        {
            selector = args.Selector,
            visible = true
        });
    }
}
