using System;
using System.Collections.Generic;
using System.Net;
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
    public bool AllowPrivateNetworks { get; set; }
}

[Keyword("http.request", Category = "HTTP", Description = "Executes an HTTP request.")]
public sealed class HttpRequestKeyword : IKeywordHandler<HttpRequestArgs>
{
    private static readonly string[] AllowedSchemes = { "http", "https" };
    
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

        var (isValid, errorMessage) = ValidateUrl(url, args.AllowPrivateNetworks);
        if (!isValid)
        {
            context.Logger.LogWarning("URL validation failed: {Error}", errorMessage);
            return KeywordResult.Failure(errorMessage!);
        }

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

    private static (bool IsValid, string? ErrorMessage) ValidateUrl(string url, bool allowPrivateNetworks)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return (false, "URL cannot be empty.");
        }

        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch (UriFormatException)
        {
            return (false, $"Invalid URL format: {url}");
        }

        if (!AllowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase))
        {
            return (false, $"URL scheme '{uri.Scheme}' is not allowed. Only http and https are permitted.");
        }

        if (!allowPrivateNetworks && IsPrivateNetwork(uri.Host))
        {
            return (false, "Access to private network addresses is disabled. Set allowPrivateNetworks: true to enable.");
        }

        return (true, null);
    }

    private static bool IsPrivateNetwork(string host)
    {
        if (!IPAddress.TryParse(host, out var ip))
        {
            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            return false;
        }

        var bytes = ip.GetAddressBytes();

        if (bytes.Length == 4)
        {
            return bytes[0] == 10 ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   bytes[0] == 127;
        }

        if (bytes.Length == 16)
        {
            return bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80;
        }

        return false;
    }
}
