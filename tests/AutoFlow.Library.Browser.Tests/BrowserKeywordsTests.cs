using AutoFlow.Abstractions;
using AutoFlow.Library.Browser;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutoFlow.Library.Browser.Tests;

public sealed class BrowserKeywordsTests : IAsyncLifetime
{
    private readonly Mock<IExecutionContext> _executionContextMock = new();
    private readonly Mock<ILogger> _loggerMock = new();
    private string _tempDirectory = string.Empty;
    private string _pagePath = string.Empty;
    private string _pageUrl = string.Empty;

    public Task InitializeAsync()
    {
        _tempDirectory = Path.Join(Path.GetTempPath(), $"autoflow_browser_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);

        _pagePath = Path.Join(_tempDirectory, "test-page.html");
        _pageUrl = new Uri(_pagePath).AbsoluteUri;

        File.WriteAllText(
            _pagePath,
            """
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <title>AutoFlow Browser Test</title>
              <style>
                #details { display: none; }
              </style>
              <script>
                function handleSubmit() {
                  const value = document.getElementById('name').value;
                  document.getElementById('status').textContent = 'Hello ' + value;
                  document.getElementById('details').style.display = 'block';
                }
                function handleKey(event) {
                  if (event.key === 'Enter') {
                    document.getElementById('pressed').textContent = 'Enter pressed';
                  }
                }
              </script>
            </head>
            <body>
              <input id="name" type="text" onkeydown="handleKey(event)" />
              <button id="submit" type="button" onclick="handleSubmit()">Submit</button>
              <div id="status">Ready</div>
              <div id="pressed"></div>
              <div id="details">Details visible</div>
            </body>
            </html>
            """);

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }

        return Task.CompletedTask;
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task BrowserKeywords_LocalPage_ExerciseCoreWorkflow()
    {
        await using var browserManager = new BrowserManager();
        var openKeyword = new BrowserOpenKeyword(browserManager);
        var gotoKeyword = new BrowserGotoKeyword(browserManager);
        var fillKeyword = new BrowserFillKeyword(browserManager);
        var pressKeyword = new BrowserPressKeyword(browserManager);
        var clickKeyword = new BrowserClickKeyword(browserManager);
        var waitKeyword = new BrowserWaitKeyword(browserManager);
        var assertVisibleKeyword = new BrowserAssertVisibleKeyword(browserManager);
        var assertTextKeyword = new BrowserAssertTextKeyword(browserManager);
        var getTextKeyword = new BrowserGetTextKeyword(browserManager);
        var evaluateKeyword = new BrowserEvaluateKeyword(browserManager);
        var closeKeyword = new BrowserCloseKeyword(browserManager);

        var openResult = await openKeyword.ExecuteAsync(CreateContext("open", "browser.open"), new BrowserOpenArgs
        {
            Browser = "chromium",
            Headless = true,
            Width = 1280,
            Height = 720
        });

        Assert.True(openResult.IsSuccess, openResult.ErrorMessage);
        var browserId = GetOutput<string>(openResult, "browserId");

        var gotoResult = await gotoKeyword.ExecuteAsync(CreateContext("goto", "browser.goto"), new BrowserGotoArgs
        {
            BrowserId = browserId,
            Url = _pageUrl
        });

        Assert.True(gotoResult.IsSuccess, gotoResult.ErrorMessage);
        Assert.Equal(_pageUrl, GetOutput<string>(gotoResult, "url"));

        var visibleResult = await assertVisibleKeyword.ExecuteAsync(CreateContext("visible", "browser.assert_visible"), new BrowserAssertVisibleArgs
        {
            BrowserId = browserId,
            Selector = "#name"
        });
        Assert.True(visibleResult.IsSuccess, visibleResult.ErrorMessage);

        var fillResult = await fillKeyword.ExecuteAsync(CreateContext("fill", "browser.fill"), new BrowserFillArgs
        {
            BrowserId = browserId,
            Selector = "#name",
            Value = "AutoFlow"
        });
        Assert.True(fillResult.IsSuccess, fillResult.ErrorMessage);

        var pressResult = await pressKeyword.ExecuteAsync(CreateContext("press", "browser.press"), new BrowserPressArgs
        {
            BrowserId = browserId,
            Selector = "#name",
            Key = "Enter"
        });
        Assert.True(pressResult.IsSuccess, pressResult.ErrorMessage);

        var assertPressResult = await assertTextKeyword.ExecuteAsync(CreateContext("assert-press", "browser.assert_text"), new BrowserAssertTextArgs
        {
            BrowserId = browserId,
            Selector = "#pressed",
            Expected = "Enter pressed",
            Contains = true
        });
        Assert.True(assertPressResult.IsSuccess, assertPressResult.ErrorMessage);

        var clickResult = await clickKeyword.ExecuteAsync(CreateContext("click", "browser.click"), new BrowserClickArgs
        {
            BrowserId = browserId,
            Selector = "#submit"
        });
        Assert.True(clickResult.IsSuccess, clickResult.ErrorMessage);

        var waitResult = await waitKeyword.ExecuteAsync(CreateContext("wait", "browser.wait"), new BrowserWaitArgs
        {
            BrowserId = browserId,
            Selector = "#details",
            State = "visible",
            TimeoutMs = 5000
        });
        Assert.True(waitResult.IsSuccess, waitResult.ErrorMessage);

        var getTextResult = await getTextKeyword.ExecuteAsync(CreateContext("get-text", "browser.get_text"), new BrowserGetTextArgs
        {
            BrowserId = browserId,
            Selector = "#status"
        });
        Assert.True(getTextResult.IsSuccess, getTextResult.ErrorMessage);
        Assert.Equal("Hello AutoFlow", GetOutput<string>(getTextResult, "text"));

        var assertTextResult = await assertTextKeyword.ExecuteAsync(CreateContext("assert-text", "browser.assert_text"), new BrowserAssertTextArgs
        {
            BrowserId = browserId,
            Selector = "#status",
            Expected = "Hello AutoFlow",
            Contains = false
        });
        Assert.True(assertTextResult.IsSuccess, assertTextResult.ErrorMessage);

        var evaluateResult = await evaluateKeyword.ExecuteAsync(CreateContext("evaluate", "browser.evaluate"), new BrowserEvaluateArgs
        {
            BrowserId = browserId,
            Script = "() => document.querySelector('#status').textContent"
        });
        Assert.True(evaluateResult.IsSuccess, evaluateResult.ErrorMessage);
        Assert.Equal("Hello AutoFlow", GetOutput<string>(evaluateResult, "result"));

        var closeResult = await closeKeyword.ExecuteAsync(CreateContext("close", "browser.close"), new BrowserCloseArgs
        {
            BrowserId = browserId
        });
        Assert.True(closeResult.IsSuccess, closeResult.ErrorMessage);
        Assert.True(GetOutput<bool>(closeResult, "closed"));
        Assert.Null(browserManager.GetPage(browserId));
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task BrowserScreenshot_CapturesPageAndElement()
    {
        await using var browserManager = new BrowserManager();
        var openKeyword = new BrowserOpenKeyword(browserManager);
        var gotoKeyword = new BrowserGotoKeyword(browserManager);
        var screenshotKeyword = new BrowserScreenshotKeyword(browserManager);
        var closeKeyword = new BrowserCloseKeyword(browserManager);

        var openResult = await openKeyword.ExecuteAsync(CreateContext("open", "browser.open"), new BrowserOpenArgs
        {
            Browser = "chromium",
            Headless = true
        });

        var browserId = GetOutput<string>(openResult, "browserId");
        var pageScreenshotPath = Path.Join(_tempDirectory, "page.png");
        var elementScreenshotPath = Path.Join(_tempDirectory, "element.png");

        try
        {
            await gotoKeyword.ExecuteAsync(CreateContext("goto", "browser.goto"), new BrowserGotoArgs
            {
                BrowserId = browserId,
                Url = _pageUrl
            });

            var pageResult = await screenshotKeyword.ExecuteAsync(CreateContext("page-shot", "browser.screenshot"), new BrowserScreenshotArgs
            {
                BrowserId = browserId,
                Path = pageScreenshotPath,
                FullPage = true
            });

            var elementResult = await screenshotKeyword.ExecuteAsync(CreateContext("element-shot", "browser.screenshot"), new BrowserScreenshotArgs
            {
                BrowserId = browserId,
                Path = elementScreenshotPath,
                Selector = "#status"
            });

            Assert.True(pageResult.IsSuccess, pageResult.ErrorMessage);
            Assert.True(elementResult.IsSuccess, elementResult.ErrorMessage);
            Assert.True(File.Exists(pageScreenshotPath));
            Assert.True(File.Exists(elementScreenshotPath));
            Assert.True(new FileInfo(pageScreenshotPath).Length > 0);
            Assert.True(new FileInfo(elementScreenshotPath).Length > 0);
        }
        finally
        {
            await closeKeyword.ExecuteAsync(CreateContext("close", "browser.close"), new BrowserCloseArgs
            {
                BrowserId = browserId
            });
        }
    }

    [Fact]
    [Trait("Category", "Browser")]
    public async Task BrowserGoto_InvalidBrowserId_ReturnsFailure()
    {
        await using var browserManager = new BrowserManager();
        var keyword = new BrowserGotoKeyword(browserManager);

        var result = await keyword.ExecuteAsync(CreateContext("goto", "browser.goto"), new BrowserGotoArgs
        {
            BrowserId = "missing-browser",
            Url = _pageUrl
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("Browser not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private KeywordContext CreateContext(string stepId, string keywordName)
    {
        return new KeywordContext
        {
            ExecutionContext = _executionContextMock.Object,
            StepId = stepId,
            KeywordName = keywordName,
            Logger = _loggerMock.Object
        };
    }

    private static T GetOutput<T>(KeywordResult result, string propertyName)
    {
        Assert.NotNull(result.Outputs);
        var property = result.Outputs!.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        var value = property!.GetValue(result.Outputs);
        Assert.NotNull(value);

        if (value is T typedValue)
        {
            return typedValue;
        }

        if (typeof(T) == typeof(string))
        {
            object converted = value.ToString()!;
            return (T)converted;
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }
}
