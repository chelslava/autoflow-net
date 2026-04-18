// =============================================================================
// BrowserManager.cs — управляет жизненным циклом браузеров и Playwright.
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace AutoFlow.Library.Browser;

/// <summary>
/// Централизованный менеджер для управления жизненным циклом браузеров.
/// Решает проблему утечки памяти Playwright и обеспечивает корректную очистку.
/// </summary>
public sealed class BrowserManager : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, BrowserInstance> _instances = new();
    private readonly SemaphoreSlim _playwrightLock = new(1, 1);
    private readonly ILogger? _logger;
    
    private IPlaywright? _playwright;
    private bool _disposed;

    public BrowserManager(ILogger? logger = null)
    {
        _logger = logger;
    }

    public async Task<BrowserInstance> CreateBrowserAsync(
        string browserType,
        bool headless = true,
        int? width = null,
        int? height = null,
        bool slowMo = false,
        bool disableJavaScript = false,
        bool ignoreHTTPSErrors = false,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _playwrightLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _playwright ??= await Playwright.CreateAsync().ConfigureAwait(false);
        }
        finally
        {
            _playwrightLock.Release();
        }

        IBrowser browser = browserType.ToLowerInvariant() switch
        {
            "firefox" => await _playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = headless,
                SlowMo = slowMo ? 100 : null
            }).ConfigureAwait(false),
            "webkit" => await _playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = headless,
                SlowMo = slowMo ? 100 : null
            }).ConfigureAwait(false),
            _ => await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = headless,
                SlowMo = slowMo ? 100 : null
            }).ConfigureAwait(false)
        };

        var viewport = width.HasValue && height.HasValue
            ? new ViewportSize { Width = width.Value, Height = height.Value }
            : null;

        var page = await browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = viewport,
            JavaScriptEnabled = !disableJavaScript,
            IgnoreHTTPSErrors = ignoreHTTPSErrors
        }).ConfigureAwait(false);

        var browserId = Guid.NewGuid().ToString("N")[..8];
        var instance = new BrowserInstance(browserId, browser, page, browserType, headless, width, height);

        _instances[browserId] = instance;

        _logger?.LogInformation("Browser opened: {BrowserId} ({BrowserType})", browserId, browserType);

        return instance;
    }

    public BrowserInstance? GetInstance(string browserId)
    {
        return _instances.TryGetValue(browserId, out var instance) ? instance : null;
    }

    public IPage? GetPage(string browserId)
    {
        return _instances.TryGetValue(browserId, out var instance) ? instance.Page : null;
    }

    public IBrowser? GetBrowser(string browserId)
    {
        return _instances.TryGetValue(browserId, out var instance) ? instance.Browser : null;
    }

    public async Task CloseBrowserAsync(string browserId)
    {
        if (!_instances.TryRemove(browserId, out var instance))
            return;

        _logger?.LogInformation("Closing browser: {BrowserId}", browserId);

        try
        {
            if (instance.Page is not null)
                await instance.Page.CloseAsync().ConfigureAwait(false);
        }
        catch (PlaywrightException ex)
        {
            _logger?.LogWarning(ex, "Error closing page for browser {BrowserId}", browserId);
        }

        try
        {
            if (instance.Browser is not null)
                await instance.Browser.CloseAsync().ConfigureAwait(false);
        }
        catch (PlaywrightException ex)
        {
            _logger?.LogWarning(ex, "Error closing browser {BrowserId}", browserId);
        }
    }

    public async Task CloseAllBrowsersAsync()
    {
        var browserIds = _instances.Keys.ToArray();
        
        foreach (var browserId in browserIds)
        {
            await CloseBrowserAsync(browserId).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        await CloseAllBrowsersAsync().ConfigureAwait(false);

        if (_playwright is not null)
        {
            if (_playwright is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _playwright.Dispose();
            }
            _playwright = null;
        }

        _playwrightLock.Dispose();
    }
}

public sealed record BrowserInstance(
    string Id,
    IBrowser Browser,
    IPage Page,
    string BrowserType,
    bool Headless,
    int? Width,
    int? Height);
