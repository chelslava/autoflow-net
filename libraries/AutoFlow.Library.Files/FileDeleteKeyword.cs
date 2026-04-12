using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Library.Files;

public sealed class FileDeleteArgs
{
    public string Path { get; set; } = string.Empty;
}

[Keyword("files.delete", Category = "Files", Description = "Удаляет файл.")]
public sealed class FileDeleteKeyword : IKeywordHandler<FileDeleteArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        FileDeleteArgs args,
        CancellationToken cancellationToken = default)
    {
        var path = args.Path;

        if (!File.Exists(path))
        {
            context.Logger.LogWarning("Файл не найден для удаления: {Path}", path);
            return Task.FromResult(
                KeywordResult.Success(
                    new { deleted = false, path },
                    [$"File not found: {path}"]));
        }

        File.Delete(path);

        context.Logger.LogInformation("Файл удалён: {Path}", path);

        return Task.FromResult(
            KeywordResult.Success(
                new { deleted = true, path },
                [$"Deleted: {path}"]));
    }
}
