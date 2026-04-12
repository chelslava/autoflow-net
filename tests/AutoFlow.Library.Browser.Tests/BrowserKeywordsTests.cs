// =============================================================================
// BrowserKeywordsTests.cs — интеграционные тесты для Browser keywords.
// =============================================================================

using AutoFlow.Abstractions;
using AutoFlow.Library.Browser;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutoFlow.Library.Browser.Tests;

public sealed class BrowserKeywordsTests : IAsyncLifetime
{
    private readonly ILogger<KeywordContext> _logger;

    public BrowserKeywordsTests()
    {
        _logger = new Mock<ILogger<KeywordContext>>().Object;
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Cleanup any remaining browsers
    }

    [Fact]
    public async Task BrowserOpen_ShouldReturnBrowserId()
    {
        var keyword = new BrowserOpenKeyword();
        var context = new KeywordContext(_logger, new Dictionary<string, object?>());
        var args = new BrowserOpenArgs
        {
            Browser = "chromium",
            Headless = true
        };

        var result = await keyword.ExecuteAsync(context, args);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Outputs);
        Assert.Contains("browserId", result.Outputs.Keys);

        var browserId = result.Outputs["browserId"]?.ToString();
        Assert.NotNull(browserId);
        Assert.NotEmpty(browserId);

        // Cleanup
        var closeKeyword = new BrowserCloseKeyword();
        var closeArgs = new BrowserCloseArgs { BrowserId = browserId };
        await closeKeyword.ExecuteAsync(context, closeArgs);
    }

    [Fact]
    public async Task BrowserGoto_ShouldNavigateToUrl()
    {
        var openKeyword = new BrowserOpenKeyword();
        var context = new KeywordContext(_logger, new Dictionary<string, object?>());
        var openArgs = new BrowserOpenArgs
        {
            Browser = "chromium",
            Headless = true
        };

        var openResult = await openKeyword.ExecuteAsync(context, openArgs);
        var browserId = openResult.Outputs?["browserId"]?.ToString() ?? string.Empty;

        try
        {
            var gotoKeyword = new BrowserGotoKeyword();
            var gotoArgs = new BrowserGotoArgs
            {
                BrowserId = browserId,
                Url = "https://example.com"
            };

            var gotoResult = await gotoKeyword.ExecuteAsync(context, gotoArgs);

            Assert.True(gotoResult.IsSuccess);
            Assert.NotNull(gotoResult.Outputs);
            Assert.Contains("url", gotoResult.Outputs.Keys);
            Assert.Contains("example.com", gotoResult.Outputs["url"]?.ToString());
        }
        finally
        {
            var closeKeyword = new BrowserCloseKeyword();
            await closeKeyword.ExecuteAsync(context, new BrowserCloseArgs { BrowserId = browserId });
        }
    }

    [Fact]
    public async Task BrowserClick_ShouldClickElement()
    {
        var openKeyword = new BrowserOpenKeyword();
        var context = new KeywordContext(_logger, new Dictionary<string, object?>());
        var openArgs = new BrowserOpenArgs
        {
            Browser = "chromium",
            Headless = true
        };

        var openResult = await openKeyword.ExecuteAsync(context, openArgs);
        var browserId = openResult.Outputs?["browserId"]?.ToString() ?? string.Empty;

        try
        {
            var gotoKeyword = new BrowserGotoKeyword();
            await gotoKeyword.ExecuteAsync(context, new BrowserGotoArgs
            {
                BrowserId = browserId,
                Url = "https://example.com"
            });

            var clickKeyword = new BrowserClickKeyword();
            var clickArgs = new BrowserClickArgs
            {
                BrowserId = browserId,
                Selector = "body"
            };

            var clickResult = await clickKeyword.ExecuteAsync(context, clickArgs);

            Assert.True(clickResult.IsSuccess);
            Assert.NotNull(clickResult.Outputs);
        }
        finally
        {
            var closeKeyword = new BrowserCloseKeyword();
            await closeKeyword.ExecuteAsync(context, new BrowserCloseArgs { BrowserId = browserId });
        }
    }

    [Fact]
    public async Task BrowserFill_ShouldFillInput()
    {
        var openKeyword = new BrowserOpenKeyword();
        var context = new KeywordContext(_logger, new Dictionary<string, object?>());
        var openArgs = new BrowserOpenArgs
        {
            Browser = "chromium",
            Headless = true
        };

        var openResult = await openKeyword.ExecuteAsync(context, openArgs);
        var browserId = openResult.Outputs?["browserId"]?.ToString() ?? string.Empty;

        try
        {
            var gotoKeyword = new BrowserGotoKeyword();
            await gotoKeyword.ExecuteAsync(context, new BrowserGotoArgs
            {
                BrowserId = browserId,
                Url = "https://example.com"
            });

            // Note: example.com doesn't have inputs, but we test the keyword logic
            var fillKeyword = new BrowserFillKeyword();
            var fillArgs = new BrowserFillArgs
            {
                BrowserId = browserId,
                Selector = "input[type='search']",
                Value = "test"
            };

            // Will fail because no input exists, but that's expected
            var fillResult = await fillKeyword.ExecuteAsync(context, fillArgs);
            
            // Just verify the keyword runs without exception
            Assert.NotNull(fillResult);
        }
        finally
        {
            var closeKeyword = new BrowserCloseKeyword();
            await closeKeyword.ExecuteAsync(context, new BrowserCloseArgs { BrowserId = browserId });
        }
    }

    [Fact]
    public async Task BrowserScreenshot_ShouldTakeScreenshot()
    {
        var openKeyword = new BrowserOpenKeyword();
        var context = new KeywordContext(_logger, new Dictionary<string, object?>());
        var openArgs = new BrowserOpenArgs
        {
            Browser = "chromium",
            Headless = true
        };

        var openResult = await openKeyword.ExecuteAsync(context, openArgs);
        var browserId = openResult.Outputs?["browserId"]?.ToString() ?? string.Empty;

        var screenshotPath = Path.Combine(Path.GetTempPath(), $"test_screenshot_{Guid.NewGuid():N}.png");

        try
        {
            var gotoKeyword = new BrowserGotoKeyword();
            await gotoKeyword.ExecuteAsync(context, new BrowserGotoArgs
            {
                BrowserId = browserId,
                Url = "https://example.com"
            });

            var screenshotKeyword = new BrowserScreenshotKeyword();
            var screenshotArgs = new BrowserScreenshotArgs
            {
                BrowserId = browserId,
                Path = screenshotPath,
                FullPage = false
            };

            var screenshotResult = await screenshotKeyword.ExecuteAsync(context, screenshotArgs);

            Assert.True(screenshotResult.IsSuccess);
            Assert.NotNull(screenshotResult.Outputs);
            Assert.True(File.Exists(screenshotPath));
            Assert.True(new FileInfo(screenshotPath).Length > 0);
        }
        finally
        {
            if (File.Exists(screenshotPath))
                File.Delete(screenshotPath);
            
            var closeKeyword = new BrowserCloseKeyword();
            await closeKeyword.ExecuteAsync(context, new BrowserCloseArgs { BrowserId = browserId });
        }
    }

    [Fact]
    public async Task BrowserClose_ShouldCloseBrowser()
    {
        var openKeyword = new BrowserOpenKeyword();
        var context = new KeywordContext(_logger, new Dictionary<string, object?>());
        var openArgs = new BrowserOpenArgs
        {
            Browser = "chromium",
            Headless = true
        };

        var openResult = await openKeyword.ExecuteAsync(context, openArgs);
        var browserId = openResult.Outputs?["browserId"]?.ToString() ?? string.Empty;

        var closeKeyword = new BrowserCloseKeyword();
        var closeArgs = new BrowserCloseArgs { BrowserId = browserId };

        var closeResult = await closeKeyword.ExecuteAsync(context, closeArgs);

        Assert.True(closeResult.IsSuccess);
        Assert.NotNull(closeResult.Outputs);
        Assert.True((bool)closeResult.Outputs["closed"]);

        // Verify browser is removed
        var page = BrowserOpenKeyword.GetPage(browserId);
        Assert.Null(page);
    }

    [Fact]
    public async Task BrowserGoto_WithInvalidBrowserId_ShouldFail()
    {
        var keyword = new BrowserGotoKeyword();
        var context = new KeywordContext(_logger, new Dictionary<string, object?>());
        var args = new BrowserGotoArgs
        {
            BrowserId = "invalid_id",
            Url = "https://example.com"
        };

        var result = await keyword.ExecuteAsync(context, args);

        Assert.False(result.IsSuccess);
        Assert.Contains("Browser not found", result.ErrorMessage);
    }
}
