using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Library.Http;

public sealed class HttpRequestArgs
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public Dictionary<string, string>? Headers { get; set; }
    public object? Body { get; set; }
    public int? TimeoutMs { get; set; }
}

[Keyword("http.request", Category = "HTTP", Description = "Выполняет HTTP-запрос.")]
public sealed class HttpRequestKeyword : IKeywordHandler<HttpRequestArgs>
{
    private readonly HttpClient _httpClient;

    public HttpRequestKeyword(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        HttpRequestArgs args,
        CancellationToken cancellationToken = default)
    {
        var url = args.Url;
        var method = args.Method?.ToUpperInvariant() ?? "GET";

        using var request = new HttpRequestMessage(
            new HttpMethod(method),
            url);

        if (args.Headers is not null)
        {
            foreach (var (key, value) in args.Headers)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

        if (args.Body is not null && method is "POST" or "PUT" or "PATCH")
        {
            var json = JsonSerializer.Serialize(args.Body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        context.Logger.LogInformation(
            "HTTP {Method} {Url}",
            method, url);

        using var cts = args.TimeoutMs.HasValue
            ? new CancellationTokenSource(args.TimeoutMs.Value)
            : null;

        using var linkedCts = cts is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token)
            : null;

        var effectiveToken = linkedCts?.Token ?? cancellationToken;

        var response = await _httpClient.SendAsync(request, effectiveToken).ConfigureAwait(false);

        var responseBody = await response.Content.ReadAsStringAsync(effectiveToken).ConfigureAwait(false);

        var result = new
        {
            statusCode = (int)response.StatusCode,
            statusText = response.StatusCode.ToString(),
            headers = response.Headers,
            body = responseBody,
            isSuccess = response.IsSuccessStatusCode
        };

        context.Logger.LogInformation(
            "HTTP {Method} {Url} -> {StatusCode}",
            method, url, (int)response.StatusCode);

        var logs = new List<string>
        {
            $"{method} {url}",
            $"Status: {(int)response.StatusCode} {response.StatusCode}",
            $"Body: {responseBody}"
        };

        return response.IsSuccessStatusCode
            ? KeywordResult.Success(result, logs)
            : KeywordResult.Failure($"HTTP {response.StatusCode}: {responseBody}", logs);
    }
}
