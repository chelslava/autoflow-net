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
    public bool DisableJavaScript { get; set; } = false;
    public bool IgnoreHTTPSErrors { get; set; } = false;
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

        try
        {
            var instance = await _browserManager.CreateBrowserAsync(
                args.Browser,
                args.Headless,
                args.Width,
                args.Height,
                args.SlowMo,
                args.DisableJavaScript,
                args.IgnoreHTTPSErrors,
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
        catch (PlaywrightException ex)
        {
            context.Logger.LogError(ex, "Failed to open browser: {Message}", ex.Message);
            return KeywordResult.Failure(
                $"Failed to open {args.Browser} browser: {ex.Message}. " +
                "Ensure Playwright browsers are installed. Run: pwsh playwright.ps1 install");
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Unexpected error opening browser");
            return KeywordResult.Failure(
                $"Unexpected error opening browser: {ex.Message}. " +
                "Check logs for details.");
        }
    }

    public static async Task<IPage?> GetPageAsync(string browserId) =>
        await BrowserManagerProvider.WithManagerAsync(async m => 
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return m.GetPage(browserId);
        }).ConfigureAwait(false);

    public static async Task<IBrowser?> GetBrowserAsync(string browserId) =>
        await BrowserManagerProvider.WithManagerAsync(async m =>
        {
            await Task.CompletedTask.ConfigureAwait(false);
            return m.GetBrowser(browserId);
        }).ConfigureAwait(false);

    public static async Task CloseBrowserAsync(string browserId)
    {
        await BrowserManagerProvider.WithManagerAsync(async m =>
        {
            await m.CloseBrowserAsync(browserId).ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);
    }
}
