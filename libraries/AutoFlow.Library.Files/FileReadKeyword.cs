using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Library.Files;

public sealed class FileReadArgs
{
    public string Path { get; set; } = string.Empty;
    public string? Encoding { get; set; }
    public string? BasePath { get; set; }
    public long? MaxSizeBytes { get; set; }
}

[Keyword("files.read", Category = "Files", Description = "Reads file contents into a string.")]
public sealed class FileReadKeyword : IKeywordHandler<FileReadArgs>
{
    private const long DefaultMaxFileSize = 10 * 1024 * 1024; // 10 MB

    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        FileReadArgs args,
        CancellationToken cancellationToken = default)
    {
        var basePath = PathValidator.GetAllowedBasePath(args.BasePath);
        var (isValid, fullPath, errorMessage) = PathValidator.ValidatePath(args.Path, basePath);

        if (!isValid)
        {
            context.Logger.LogWarning("Path validation failed: {Error}", errorMessage);
            return Task.FromResult(KeywordResult.Failure(errorMessage ?? "Invalid path"));
        }

        if (!File.Exists(fullPath))
        {
            return Task.FromResult(
                KeywordResult.Failure($"File not found: {args.Path}"));
        }

        var maxSize = args.MaxSizeBytes ?? DefaultMaxFileSize;
        var fileInfo = new FileInfo(fullPath);
        
        if (fileInfo.Length > maxSize)
        {
            return Task.FromResult(
                KeywordResult.Failure($"File size ({fileInfo.Length:N0} bytes) exceeds maximum allowed ({maxSize:N0} bytes)"));
        }

        var encoding = args.Encoding?.ToLowerInvariant() switch
        {
            "utf8" or "utf-8" => System.Text.Encoding.UTF8,
            "ascii" => System.Text.Encoding.ASCII,
            _ => System.Text.Encoding.UTF8
        };

        var content = File.ReadAllText(fullPath, encoding);

        context.Logger.LogInformation(
            "Read file {Path}, size: {Size} characters",
            args.Path, content.Length);

        return Task.FromResult(
            KeywordResult.Success(
                new { content, path = args.Path, sizeBytes = fileInfo.Length },
                [$"Read {content.Length} chars from {args.Path}"]));
    }
}
