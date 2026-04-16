using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Library.Files;

public sealed class FileDeleteArgs
{
    public string Path { get; set; } = string.Empty;
    public string? BasePath { get; set; }
}

[Keyword("files.delete", Category = "Files", Description = "Deletes a file.")]
public sealed class FileDeleteKeyword : IKeywordHandler<FileDeleteArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        FileDeleteArgs args,
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
            context.Logger.LogWarning("File not found for deletion: {Path}", args.Path);
            return Task.FromResult(
                KeywordResult.Success(
                    new { deleted = false, path = args.Path },
                    [$"File not found: {args.Path}"]));
        }

        File.Delete(fullPath);

        context.Logger.LogInformation("File deleted: {Path}", args.Path);

        return Task.FromResult(
            KeywordResult.Success(
                new { deleted = true, path = args.Path },
                [$"Deleted: {args.Path}"]));
    }
}
