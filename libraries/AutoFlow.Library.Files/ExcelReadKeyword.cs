using System.IO.Compression;
using System.Xml.Linq;
using AutoFlow.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoFlow.Library.Files;

public sealed class ExcelReadArgs
{
    public string Path { get; set; } = string.Empty;
    public string? BasePath { get; set; }
    public bool TrimHeaders { get; set; } = true;
    public bool SkipEmptyRows { get; set; } = true;
}

[Keyword("excel.read", Category = "Files", Description = "Reads rows from the first worksheet of an .xlsx file using the first row as headers.")]
public sealed class ExcelReadKeyword : IKeywordHandler<ExcelReadArgs>
{
    private static readonly XNamespace SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";

    public Task<KeywordResult> ExecuteAsync(
        KeywordContext context,
        ExcelReadArgs args,
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
            return Task.FromResult(KeywordResult.Failure($"File not found: {args.Path}"));
        }

        if (!string.Equals(Path.GetExtension(fullPath), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(KeywordResult.Failure("Only .xlsx files are supported."));
        }

        try
        {
            using var archive = ZipFile.OpenRead(fullPath);
            var sharedStrings = LoadSharedStrings(archive);
            var worksheetPath = ResolveFirstWorksheetPath(archive);
            var rows = ReadRows(archive, worksheetPath, sharedStrings, args.TrimHeaders, args.SkipEmptyRows);

            context.Logger.LogInformation(
                "Read Excel workbook {Path}, rows: {Count}",
                args.Path, rows.Count);

            return Task.FromResult(
                KeywordResult.Success(
                    new
                    {
                        rows,
                        count = rows.Count,
                        path = args.Path
                    },
                    [$"Read {rows.Count} rows from {args.Path}"]));
        }
        catch (InvalidDataException ex)
        {
            return Task.FromResult(KeywordResult.Failure($"Excel read error: {ex.Message}"));
        }
    }

    private static List<Dictionary<string, object?>> ReadRows(
        ZipArchive archive,
        string worksheetPath,
        IReadOnlyList<string> sharedStrings,
        bool trimHeaders,
        bool skipEmptyRows)
    {
        var worksheetEntry = archive.GetEntry(worksheetPath)
            ?? throw new InvalidDataException($"Worksheet entry not found: {worksheetPath}");

        using var stream = worksheetEntry.Open();
        var worksheet = XDocument.Load(stream);
        var rowElements = worksheet.Descendants(SpreadsheetNamespace + "row").ToList();

        if (rowElements.Count == 0)
        {
            return new List<Dictionary<string, object?>>();
        }

        var headerValues = ReadRowValues(rowElements[0], sharedStrings, trimHeaders);
        var headerMap = BuildHeaderMap(headerValues);

        if (headerMap.Count == 0)
        {
            throw new InvalidDataException("Excel worksheet does not contain any headers.");
        }

        var rows = new List<Dictionary<string, object?>>();

        foreach (var rowElement in rowElements.Skip(1))
        {
            var rowValues = ReadRowValues(rowElement, sharedStrings, false);
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var (columnIndex, headerName) in headerMap)
            {
                row[headerName] = rowValues.TryGetValue(columnIndex, out var value)
                    ? value
                    : string.Empty;
            }

            if (skipEmptyRows && row.Values.All(value => string.IsNullOrWhiteSpace(value?.ToString())))
            {
                continue;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static Dictionary<int, string> BuildHeaderMap(Dictionary<int, string> headerValues)
    {
        var headerMap = new Dictionary<int, string>();
        var seenHeaders = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var (columnIndex, headerValue) in headerValues.OrderBy(pair => pair.Key))
        {
            if (string.IsNullOrWhiteSpace(headerValue))
            {
                continue;
            }

            var normalizedHeader = headerValue.Trim();
            if (seenHeaders.TryGetValue(normalizedHeader, out var currentCount))
            {
                currentCount++;
                seenHeaders[normalizedHeader] = currentCount;
                normalizedHeader = $"{normalizedHeader}_{currentCount}";
            }
            else
            {
                seenHeaders[normalizedHeader] = 1;
            }

            headerMap[columnIndex] = normalizedHeader;
        }

        return headerMap;
    }

    private static Dictionary<int, string> ReadRowValues(
        XElement rowElement,
        IReadOnlyList<string> sharedStrings,
        bool trimHeaders)
    {
        var values = new Dictionary<int, string>();
        var nextColumnIndex = 0;

        foreach (var cell in rowElement.Elements(SpreadsheetNamespace + "c"))
        {
            var reference = cell.Attribute("r")?.Value;
            var columnIndex = reference is not null
                ? GetColumnIndex(reference)
                : nextColumnIndex;

            nextColumnIndex = columnIndex + 1;
            var cellValue = ReadCellValue(cell, sharedStrings);
            values[columnIndex] = trimHeaders ? cellValue.Trim() : cellValue;
        }

        return values;
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var cellType = cell.Attribute("t")?.Value;

        return cellType switch
        {
            "s" => ReadSharedString(cell, sharedStrings),
            "inlineStr" => string.Concat(
                cell.Descendants(SpreadsheetNamespace + "t").Select(textNode => textNode.Value)),
            "b" => cell.Element(SpreadsheetNamespace + "v")?.Value == "1" ? "true" : "false",
            _ => cell.Element(SpreadsheetNamespace + "v")?.Value ?? string.Empty
        };
    }

    private static string ReadSharedString(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var rawValue = cell.Element(SpreadsheetNamespace + "v")?.Value;
        if (!int.TryParse(rawValue, out var index) || index < 0 || index >= sharedStrings.Count)
        {
            return string.Empty;
        }

        return sharedStrings[index];
    }

    private static List<string> LoadSharedStrings(ZipArchive archive)
    {
        var sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
        if (sharedStringsEntry is null)
        {
            return new List<string>();
        }

        using var stream = sharedStringsEntry.Open();
        var sharedStringsDocument = XDocument.Load(stream);

        return sharedStringsDocument
            .Descendants(SpreadsheetNamespace + "si")
            .Select(sharedStringItem => string.Concat(
                sharedStringItem.Descendants(SpreadsheetNamespace + "t").Select(textNode => textNode.Value)))
            .ToList();
    }

    private static string ResolveFirstWorksheetPath(ZipArchive archive)
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml")
            ?? throw new InvalidDataException("Workbook entry not found.");
        var workbookRelationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels")
            ?? throw new InvalidDataException("Workbook relationships entry not found.");

        using var workbookStream = workbookEntry.Open();
        using var relationshipsStream = workbookRelationshipsEntry.Open();

        var workbook = XDocument.Load(workbookStream);
        var relationships = XDocument.Load(relationshipsStream);

        var firstSheet = workbook
            .Descendants(SpreadsheetNamespace + "sheet")
            .FirstOrDefault()
            ?? throw new InvalidDataException("Workbook does not contain any worksheets.");

        var relationshipId = firstSheet.Attribute(RelationshipNamespace + "id")?.Value;
        if (string.IsNullOrWhiteSpace(relationshipId))
        {
            throw new InvalidDataException("Worksheet relationship id is missing.");
        }

        var relationship = relationships
            .Descendants(PackageRelationshipNamespace + "Relationship")
            .FirstOrDefault(item => string.Equals(item.Attribute("Id")?.Value, relationshipId, StringComparison.Ordinal));

        var target = relationship?.Attribute("Target")?.Value;
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidDataException($"Worksheet target not found for relationship '{relationshipId}'.");
        }

        return NormalizeWorksheetPath(target);
    }

    private static string NormalizeWorksheetPath(string target)
    {
        var normalizedTarget = target.Replace('\\', '/').TrimStart('/');

        return normalizedTarget.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)
            ? normalizedTarget
            : $"xl/{normalizedTarget}";
    }

    private static int GetColumnIndex(string cellReference)
    {
        var columnLetters = new string(cellReference
            .TakeWhile(character => char.IsLetter(character))
            .ToArray());

        if (string.IsNullOrWhiteSpace(columnLetters))
        {
            return 0;
        }

        var columnIndex = 0;
        foreach (var character in columnLetters.ToUpperInvariant())
        {
            columnIndex = (columnIndex * 26) + (character - 'A' + 1);
        }

        return columnIndex - 1;
    }
}
