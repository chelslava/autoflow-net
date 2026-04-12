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
}

[Keyword("files.read", Category = "Files", Description = "Читает содержимое файла в строку.")]
public sealed class FileReadKeyword : IKeywordHandler<FileReadArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        FileReadArgs args,
        CancellationToken cancellationToken = default)
    {
        var path = args.Path;

        if (!File.Exists(path))
        {
            return Task.FromResult(
                KeywordResult.Failure($"Файл не найден: {path}"));
        }

        var encoding = args.Encoding?.ToLowerInvariant() switch
        {
            "utf8" or "utf-8" => System.Text.Encoding.UTF8,
            "ascii" => System.Text.Encoding.ASCII,
            _ => System.Text.Encoding.UTF8
        };

        var content = File.ReadAllText(path, encoding);

        context.Logger.LogInformation(
            "Прочитан файл {Path}, размер: {Size} символов",
            path, content.Length);

        return Task.FromResult(
            KeywordResult.Success(
                new { content, path },
                [$"Read {content.Length} chars from {path}"]));
    }
}
