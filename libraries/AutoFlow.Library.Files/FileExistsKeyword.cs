using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Library.Files;

public sealed class FileExistsArgs
{
    public string Path { get; set; } = string.Empty;
    public string? BasePath { get; set; }
}

[Keyword("files.exists", Category = "Files", Description = "Checks if a file exists.")]
public sealed class FileExistsKeyword : IKeywordHandler<FileExistsArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        FileExistsArgs args,
        CancellationToken cancellationToken = default)
    {
        var basePath = PathValidator.GetAllowedBasePath(args.BasePath);
        var (isValid, fullPath, errorMessage) = PathValidator.ValidatePath(args.Path, basePath);

        if (!isValid)
        {
            context.Logger.LogWarning("Path validation failed: {Error}", errorMessage);
            return Task.FromResult(KeywordResult.Failure(errorMessage ?? "Invalid path"));
        }

        var exists = File.Exists(fullPath);

        context.Logger.LogInformation(
            "Checked file {Path}: {Exists}",
            args.Path, exists ? "exists" : "not found");

        return Task.FromResult(
            KeywordResult.Success(
                new { exists, path = args.Path },
                [$"File exists: {exists}"]));
    }
}
