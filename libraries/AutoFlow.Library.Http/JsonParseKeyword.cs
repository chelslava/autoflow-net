using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Library.Http;

public sealed class JsonParseArgs
{
    public string Json { get; set; } = string.Empty;
    public string? Path { get; set; }
}

[Keyword("json.parse", Category = "JSON", Description = "Парсит JSON строку и извлекает значение по пути.")]
public sealed class JsonParseKeyword : IKeywordHandler<JsonParseArgs>
{
    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        JsonParseArgs args,
        CancellationToken cancellationToken = default)
    {
        var json = args.Json;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (string.IsNullOrWhiteSpace(args.Path))
            {
                var value = JsonSerializer.Deserialize<object>(json);
                return Task.FromResult(
                    KeywordResult.Success(
                        new { value },
                        ["JSON parsed successfully"]));
            }

            var current = root;
            var segments = args.Path.Split('.');

            foreach (var segment in segments)
            {
                if (current.ValueKind == JsonValueKind.Object)
                {
                    if (!current.TryGetProperty(segment, out var property))
                    {
                        return Task.FromResult(
                            KeywordResult.Failure($"Path not found: {args.Path}"));
                    }
                    current = property;
                }
                else if (current.ValueKind == JsonValueKind.Array)
                {
                    if (!int.TryParse(segment, out var index))
                    {
                        return Task.FromResult(
                            KeywordResult.Failure($"Invalid array index: {segment}"));
                    }

                    var array = current.EnumerateArray();
                    var i = 0;
                    foreach (var element in array)
                    {
                        if (i == index)
                        {
                            current = element;
                            break;
                        }
                        i++;
                    }

                    if (i < index)
                    {
                        return Task.FromResult(
                            KeywordResult.Failure($"Array index out of bounds: {index}"));
                    }
                }
                else
                {
                    return Task.FromResult(
                        KeywordResult.Failure($"Cannot navigate path at: {segment}"));
                }
            }

            var result = current.ValueKind switch
            {
                JsonValueKind.String => current.GetString(),
                JsonValueKind.Number => current.TryGetInt32(out var intVal) ? intVal : current.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Object => JsonSerializer.Deserialize<object>(current.GetRawText()),
                JsonValueKind.Array => JsonSerializer.Deserialize<object[]>(current.GetRawText()),
                _ => current.ToString()
            };

            context.Logger.LogInformation(
                "JSON parsed, path: {Path}",
                args.Path ?? "root");

            return Task.FromResult(
                KeywordResult.Success(
                    new { value = result, path = args.Path },
                    [$"Extracted: {args.Path}"]));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(
                KeywordResult.Failure($"JSON parse error: {ex.Message}"));
        }
    }
}
