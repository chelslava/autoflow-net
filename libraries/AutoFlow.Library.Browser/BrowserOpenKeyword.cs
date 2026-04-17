// =============================================================================
// BrowserOpenKeyword.cs — открывает браузер и создаёт новую страницу.
// =============================================================================

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoFlow.Library.Browser;

public sealed class BrowserOpenArgs
{
    public string Browser { get; set; } = "chromium";
    public bool Headless { get; set; } = true;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool SlowMo { get; set; } = false;
}

[Keyword("browser.open", Category = "Browser", Description = "Открывает браузер и создаёт новую страницу.")]
public sealed class BrowserOpenKeyword : IKeywordHandler<BrowserOpenArgs>
{
    private readonly BrowserManager _browserManager;

    public BrowserOpenKeyword(BrowserManager browserManager)
    {
        _browserManager = browserManager ?? throw new ArgumentNullException(nameof(browserManager));
    }

    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserOpenArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Opening {Browser} browser (headless: {Headless})",
            args.Browser, args.Headless);

        var instance = await _browserManager.CreateBrowserAsync(
            args.Browser,
            args.Headless,
            args.Width,
            args.Height,
            args.SlowMo,
            cancellationToken).ConfigureAwait(false);

        context.Logger.LogInformation(
            "Browser opened with ID: {BrowserId}",
            instance.Id);

        return KeywordResult.Success(new
        {
            browserId = instance.Id,
            browser = instance.BrowserType,
            headless = instance.Headless,
            width = instance.Width ?? 1280,
            height = instance.Height ?? 720
        });
    }

    public static IPage? GetPage(string browserId) =>
        BrowserManagerProvider.Manager?.GetPage(browserId);

    public static IBrowser? GetBrowser(string browserId) =>
        BrowserManagerProvider.Manager?.GetBrowser(browserId);

    public static async Task CloseBrowserAsync(string browserId) =>
        await (BrowserManagerProvider.Manager?.CloseBrowserAsync(browserId) ?? Task.CompletedTask).ConfigureAwait(false);
}
