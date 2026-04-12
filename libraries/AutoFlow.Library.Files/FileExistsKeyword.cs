using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Library.Files;

public sealed class FileExistsArgs
{
    public string Path { get; set; } = string.Empty;
}

[Keyword("files.exists", Category = "Files", Description = "Проверяет существование файла.")]
public sealed class FileExistsKeyword : IKeywordHandler<FileExistsArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        FileExistsArgs args,
        CancellationToken cancellationToken = default)
    {
        var path = args.Path;
        var exists = File.Exists(path);

        context.Logger.LogInformation(
            "Проверка файла {Path}: {Exists}",
            path, exists ? "существует" : "не найден");

        return Task.FromResult(
            KeywordResult.Success(
                new { exists, path },
                [$"File exists: {exists}"]));
    }
}
