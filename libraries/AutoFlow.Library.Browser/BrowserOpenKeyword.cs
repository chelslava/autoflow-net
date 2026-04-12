// =============================================================================
// BrowserOpenKeyword.cs — открывает браузер и создаёт новую страницу.
// =============================================================================

using System.Collections.Generic;
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
    private static readonly Dictionary<string, IBrowser> _browsers = new();
    private static readonly Dictionary<string, IPage> _pages = new();
    private static readonly object _lock = new();

    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        BrowserOpenArgs args,
        CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "Opening {Browser} browser (headless: {Headless})",
            args.Browser, args.Headless);

        var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        
        IBrowser browser = args.Browser.ToLowerInvariant() switch
        {
            "firefox" => await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = args.Headless,
                SlowMo = args.SlowMo ? 100 : null
            }).ConfigureAwait(false),
            "webkit" => await playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = args.Headless,
                SlowMo = args.SlowMo ? 100 : null
            }).ConfigureAwait(false),
            _ => await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = args.Headless,
                SlowMo = args.SlowMo ? 100 : null
            }).ConfigureAwait(false)
        };

        var viewport = args.Width.HasValue && args.Height.HasValue
            ? new ViewportSize { Width = args.Width.Value, Height = args.Height.Value }
            : null;

        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = viewport
        }).ConfigureAwait(false);

        var browserId = System.Guid.NewGuid().ToString("N")[..8];

        lock (_lock)
        {
            _browsers[browserId] = browser;
            _pages[browserId] = page;
        }

        context.Logger.LogInformation(
            "Browser opened with ID: {BrowserId}",
            browserId);

        return KeywordResult.Success(new
        {
            browserId,
            browser = args.Browser,
            headless = args.Headless,
            width = args.Width ?? 1280,
            height = args.Height ?? 720
        });
    }

    public static IPage? GetPage(string browserId)
    {
        lock (_lock)
        {
            return _pages.TryGetValue(browserId, out var page) ? page : null;
        }
    }

    public static IBrowser? GetBrowser(string browserId)
    {
        lock (_lock)
        {
            return _browsers.TryGetValue(browserId, out var browser) ? browser : null;
        }
    }

    public static void RemoveBrowser(string browserId)
    {
        lock (_lock)
        {
            _browsers.Remove(browserId);
            _pages.Remove(browserId);
        }
    }
}
