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
}

[Keyword("files.write", Category = "Files", Description = "Записывает строку в файл.")]
public sealed class FileWriteKeyword : IKeywordHandler<FileWriteArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        FileWriteArgs args,
        CancellationToken cancellationToken = default)
    {
        var path = args.Path;
        var content = args.Content;

        var directory = Path.GetDirectoryName(path);
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
            File.AppendAllText(path, content, encoding);
        }
        else
        {
            File.WriteAllText(path, content, encoding);
        }

        context.Logger.LogInformation(
            "Записан файл {Path}, размер: {Size} символов",
            path, content.Length);

        return Task.FromResult(
            KeywordResult.Success(
                new { path, size = content.Length },
                [$"Wrote {content.Length} chars to {path}"]));
    }
}
