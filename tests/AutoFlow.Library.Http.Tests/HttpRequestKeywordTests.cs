using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutoFlow.Abstractions;
using AutoFlow.Library.Http;
using AutoFlow.Runtime.Resilience;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace AutoFlow.Library.Http.Tests;

public sealed class HttpRequestKeywordTests
{
    private readonly Mock<HttpMessageHandler> _messageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IExecutionContext> _executionContextMock;
    private readonly CircuitBreaker _circuitBreaker;

    public HttpRequestKeywordTests()
    {
        _messageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_messageHandlerMock.Object);
        _loggerMock = new Mock<ILogger>();
        _executionContextMock = new Mock<IExecutionContext>();
        _circuitBreaker = new CircuitBreaker();
    }

    private KeywordContext CreateContext()
    {
        return new KeywordContext
        {
            ExecutionContext = _executionContextMock.Object,
            StepId = "test_step",
            KeywordName = "http.request",
            Logger = _loggerMock.Object
        };
    }

    [Fact]
    public async Task JsonParseKeyword_ParseArray_ReturnsEnumerableObjects()
    {
        var keyword = new JsonParseKeyword();
        var args = new JsonParseArgs
        {
            Json = """[{"firstName":"John"},{"firstName":"Jane"}]"""
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);

        var value = result.Outputs!.GetType().GetProperty("value")!.GetValue(result.Outputs);
        var items = Assert.IsAssignableFrom<IEnumerable<object?>>(value);
        var list = items.ToList();

        Assert.Equal(2, list.Count);
        var first = Assert.IsAssignableFrom<IDictionary<string, object?>>(list[0]);
        Assert.Equal("John", first["firstName"]);
    }

    [Fact]
    public async Task JsonParseKeyword_ParseObjectPath_ReturnsDictionary()
    {
        var keyword = new JsonParseKeyword();
        var args = new JsonParseArgs
        {
            Json = """{"employee":{"firstName":"John","age":30}}""",
            Path = "employee"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);

        var value = result.Outputs!.GetType().GetProperty("value")!.GetValue(result.Outputs);
        var employee = Assert.IsAssignableFrom<IDictionary<string, object?>>(value);

        Assert.Equal("John", employee["firstName"]);
        Assert.Equal(30, Convert.ToInt32(employee["age"]));
    }

    [Fact]
    public async Task ExecuteAsync_SaveToFile_WritesResponseBytes()
    {
        var tempRoot = Path.Join(Path.GetTempPath(), $"autoflow_http_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var previousDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempRoot);
            _messageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken ct) =>
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("file-content", System.Text.Encoding.UTF8, "text/plain")
                    };
                });

            var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
            var args = new HttpRequestArgs
            {
                Url = "https://api.example.com/file",
                Method = "GET",
                SaveToFile = "downloads/result.txt"
            };

            var result = await keyword.ExecuteAsync(CreateContext(), args);

            Console.WriteLine($"Result IsSuccess: {result.IsSuccess}");
            Console.WriteLine($"Result Error: {result.ErrorMessage}");
            Assert.True(result.IsSuccess, $"Result is not success. Error: {result.ErrorMessage}");
            var resultPath = Path.Join(tempRoot, "downloads", "result.txt");
            Assert.True(File.Exists(resultPath), $"File not found: {resultPath}");
            Assert.Equal("file-content", await File.ReadAllTextAsync(resultPath));
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_ValidHttpsUrl_ReturnsSuccess()
    {
        SetupHttpResponse(HttpStatusCode.OK, "{\"result\": \"ok\"}");

        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "https://api.example.com/test",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Outputs);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyUrl_ReturnsFailure()
    {
        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
        Assert.Contains("empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidUrlFormat_ReturnsFailure()
    {
        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "not-a-valid-url",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid URL format", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_FileScheme_ReturnsFailure()
    {
        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "file:///etc/passwd",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
        Assert.Contains("not allowed", result.ErrorMessage);
        Assert.Contains("http", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_FtpScheme_ReturnsFailure()
    {
        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "ftp://ftp.example.com/file.txt",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
        Assert.Contains("not allowed", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_JavaScriptScheme_ReturnsFailure()
    {
        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "javascript:alert('xss')",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_Localhost_ReturnsFailure()
    {
        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "http://localhost/admin",
            Method = "GET",
            AllowPrivateNetworks = false
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
        Assert.Contains("private network", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_LocalhostWithPort_ReturnsFailure()
    {
        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "http://localhost:8080/api",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_LocalhostIP_ReturnsFailure()
    {
        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "http://127.0.0.1/admin",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
        Assert.Contains("private network", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_PrivateIPv4_10Range_ReturnsFailure()
    {
        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "http://10.0.0.1/internal",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_PrivateIPv4_172Range_ReturnsFailure()
    {
        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "http://172.16.0.1/internal",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_PrivateIPv4_192Range_ReturnsFailure()
    {
        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "http://192.168.1.1/internal",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_LocalDomain_ReturnsFailure()
    {
        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "http://test.local/api",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_InternalDomain_ReturnsFailure()
    {
        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "http://service.internal/api",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_LocalhostWithAllowPrivateNetworks_ReturnsSuccess()
    {
        SetupHttpResponse(HttpStatusCode.OK, "{\"result\": \"ok\"}");

        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "http://localhost/test",
            Method = "GET",
            AllowPrivateNetworks = true
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_PrivateIPWithAllowPrivateNetworks_ReturnsSuccess()
    {
        SetupHttpResponse(HttpStatusCode.OK, "{\"result\": \"ok\"}");

        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "http://192.168.1.1/api",
            Method = "GET",
            AllowPrivateNetworks = true
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_PostWithBody_SendsJsonBody()
    {
        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponse(HttpStatusCode.OK, "{\"id\": 1}", capture => capturedRequest = capture);

        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "https://api.example.com/users",
            Method = "POST",
            Body = new { name = "test", email = "test@example.com" }
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest!.Content);
        Assert.Equal("application/json", capturedRequest.Content!.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ExecuteAsync_WithHeaders_AddsHeaders()
    {
        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponse(HttpStatusCode.OK, "{}", capture => capturedRequest = capture);

        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "https://api.example.com/test",
            Method = "GET",
            Headers = new()
            {
                ["Authorization"] = "Bearer token123",
                ["X-Custom-Header"] = "custom-value"
            }
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest!.Headers.Contains("Authorization"));
        Assert.True(capturedRequest.Headers.Contains("X-Custom-Header"));
    }

    [Fact]
    public async Task ExecuteAsync_NonSuccessStatusCode_ReturnsFailure()
    {
        SetupHttpResponse(HttpStatusCode.NotFound, "{\"error\": \"Not found\"}");

        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "https://api.example.com/nonexistent",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
        Assert.Contains("NotFound", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ServerError_ReturnsFailure()
    {
        SetupHttpResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

        var keyword = new HttpRequestKeyword(_httpClient, _circuitBreaker);
        var args = new HttpRequestArgs
        {
            Url = "https://api.example.com/error",
            Method = "GET"
        };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
        Assert.Contains("InternalServerError", result.ErrorMessage);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content, Action<HttpRequestMessage>? capture = null)
    {
        _messageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken ct) =>
            {
                capture?.Invoke(request);
                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
                };
            });
    }
}
