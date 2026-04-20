using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Library.Files;

public sealed class FileWriteArgs
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool Append { get; set; }
    public string? Encoding { get; set; }
    public string? BasePath { get; set; }
}

[Keyword("files.write", Category = "Files", Description = "Writes a string to a file.")]
public sealed class FileWriteKeyword : IKeywordHandler<FileWriteArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        FileWriteArgs args,
        CancellationToken cancellationToken = default)
    {
        var basePath = PathValidator.GetAllowedBasePath(args.BasePath);
        var (isValid, fullPath, errorMessage) = PathValidator.ValidatePath(args.Path, basePath);

        if (!isValid)
        {
            context.Logger.LogWarning("Path validation failed: {Error}", errorMessage);
            return Task.FromResult(KeywordResult.Failure(errorMessage ?? "Invalid path"));
        }

        var content = args.Content ?? string.Empty;
        var directory = Path.GetDirectoryName(fullPath);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var encoding = args.Encoding?.ToLowerInvariant() switch
        {
            "utf8" or "utf-8" => System.Text.Encoding.UTF8,
            "ascii" => System.Text.Encoding.ASCII,
            _ => System.Text.Encoding.UTF8
        };

        if (args.Append)
        {
            File.AppendAllText(fullPath!, content, encoding);
        }
        else
        {
            File.WriteAllText(fullPath!, content, encoding);
        }

        context.Logger.LogInformation(
            "Wrote file {Path}, size: {Size} characters",
            args.Path, content.Length);

        return Task.FromResult(
            KeywordResult.Success(
                new { path = args.Path, size = content.Length },
                [$"Wrote {content.Length} chars to {args.Path}"]));
    }
}
