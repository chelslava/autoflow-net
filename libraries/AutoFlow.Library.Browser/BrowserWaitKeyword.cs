// =============================================================================
// BrowserWaitKeyword.cs — ожидает появление элемента.
// =============================================================================

using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoFlow.Library.Browser;

public sealed class BrowserWaitArgs
{
    public string BrowserId { get; set; } = string.Empty;
    public string Selector { get; set; } = string.Empty;
    public string State { get; set; } = "visible";
    public int? TimeoutMs { get; set; }
}

[Keyword("browser.wait", Category = "Browser", Description = "Ожидает появление элемента.")]
public sealed class BrowserWaitKeyword : IKeywordHandler<BrowserWaitArgs>
{
    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserWaitArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Waiting for element: {Selector} (state: {State}, browser: {BrowserId})",
            args.Selector, args.State, args.BrowserId);

        var page = BrowserOpenKeyword.GetPage(args.BrowserId);

        if (page is null)
        {
            return KeywordResult.Failure($"Browser not found: {args.BrowserId}");
        }

        var state = args.State.ToLowerInvariant() switch
        {
            "attached" => WaitForSelectorState.Attached,
            "detached" => WaitForSelectorState.Detached,
            "hidden" => WaitForSelectorState.Hidden,
            _ => WaitForSelectorState.Visible
        };

        var options = new PageWaitForSelectorOptions
        {
            State = state,
            Timeout = args.TimeoutMs
        };

        await page.WaitForSelectorAsync(args.Selector, options).ConfigureAwait(false);

        context.Logger.LogInformation(
            "Element found: {Selector}",
            args.Selector);

        return KeywordResult.Success(new
        {
            selector = args.Selector,
            state = args.State,
            found = true
        });
    }
}
