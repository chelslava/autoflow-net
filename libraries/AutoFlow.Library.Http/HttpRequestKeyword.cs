using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
    public long? MaxResponseSizeBytes { get; set; }
    public bool LogResponseBody { get; set; }
    public int MaxLogBodyLength { get; set; } = 1024;
}

[Keyword("http.request", Category = "HTTP", Description = "Executes an HTTP request.")]
public sealed class HttpRequestKeyword : IKeywordHandler<HttpRequestArgs>
{
    private static readonly string[] AllowedSchemes = { "http", "https" };
    private const long DefaultMaxResponseSize = 50 * 1024 * 1024; // 50 MB
    
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

        var maxResponseSize = args.MaxResponseSizeBytes ?? DefaultMaxResponseSize;
        string responseBody;
        
        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength.HasValue && contentLength.Value > maxResponseSize)
        {
            responseBody = $"[Response too large: {contentLength.Value:N0} bytes > {maxResponseSize:N0} byte limit]";
        }
        else
        {
            using var stream = await response.Content.ReadAsStreamAsync(effectiveToken).ConfigureAwait(false);
            using var limitedStream = new LimitedStream(stream, maxResponseSize);
            using var reader = new StreamReader(limitedStream, Encoding.UTF8);
            responseBody = await reader.ReadToEndAsync(effectiveToken).ConfigureAwait(false);
        }

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
            $"Status: {(int)response.StatusCode} {response.StatusCode}"
        };

        if (args.LogResponseBody && !string.IsNullOrEmpty(responseBody))
        {
            var bodyToLog = responseBody.Length > args.MaxLogBodyLength
                ? responseBody[..args.MaxLogBodyLength] + "...[truncated]"
                : responseBody;
            logs.Add($"Body: {bodyToLog}");
        }

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
            
            try
            {
                var addresses = Dns.GetHostAddresses(host);
                foreach (var addr in addresses)
                {
                    if (IsPrivateIPAddress(addr))
                        return true;
                }
            }
            catch (SocketException)
            {
                // DNS resolution failed, allow the request to proceed
                // The actual connection will fail if host is unreachable
            }
            
            return false;
        }

        return IsPrivateIPAddress(ip);
    }

    private static bool IsPrivateIPAddress(IPAddress ip)
    {
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

internal sealed class LimitedStream : Stream
{
    private readonly Stream _innerStream;
    private readonly long _maxLength;
    private long _bytesRead;

    public LimitedStream(Stream innerStream, long maxLength)
    {
        _innerStream = innerStream;
        _maxLength = maxLength;
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _innerStream.Length;
    public override long Position
    {
        get => _innerStream.Position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var remaining = _maxLength - _bytesRead;
        if (remaining <= 0) return 0;
        
        var toRead = (int)Math.Min(count, remaining);
        var read = _innerStream.Read(buffer, offset, toRead);
        _bytesRead += read;
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var remaining = _maxLength - _bytesRead;
        if (remaining <= 0) return 0;
        
        var toRead = (int)Math.Min(count, remaining);
        var read = await _innerStream.ReadAsync(buffer, offset, toRead, cancellationToken).ConfigureAwait(false);
        _bytesRead += read;
        return read;
    }

    public override void Flush() => _innerStream.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
