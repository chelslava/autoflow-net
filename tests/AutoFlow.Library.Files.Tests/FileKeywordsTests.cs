using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using AutoFlow.Library.Files;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AutoFlow.Library.Files.Tests;

public sealed class FileKeywordsTests : IDisposable
{
    private readonly string _testDir;
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IExecutionContext> _executionContextMock;

    public FileKeywordsTests()
    {
        _testDir = Path.Join(Path.GetTempPath(), $"autoflow_file_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);

        _loggerMock = new Mock<ILogger>();
        _executionContextMock = new Mock<IExecutionContext>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    private KeywordContext CreateContext()
    {
        return new KeywordContext
        {
            ExecutionContext = _executionContextMock.Object,
            StepId = "test_step",
            KeywordName = "files.test",
            Logger = _loggerMock.Object
        };
    }

    #region PathValidator Tests

    [Fact]
    public void PathValidator_PathTraversal_ReturnsInvalid()
    {
        var result = PathValidator.ValidatePath("../../../etc/passwd", _testDir);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void PathValidator_ValidRelativePath_ReturnsValid()
    {
        var testFile = Path.Join(_testDir, "test.txt");
        File.WriteAllText(testFile, "test");

        var result = PathValidator.ValidatePath("test.txt", _testDir);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void PathValidator_EmptyPath_ReturnsInvalid()
    {
        var result = PathValidator.ValidatePath("", _testDir);
        Assert.False(result.IsValid);
    }

    #endregion

    #region FileWrite Tests

    [Fact]
    public async Task FileWrite_NewFile_CreatesFile()
    {
        var keyword = new FileWriteKeyword();
        var args = new FileWriteArgs { Path = "new.txt", Content = "New content", BasePath = _testDir };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
        var fullPath = Path.Join(_testDir, "new.txt");
        Assert.True(File.Exists(fullPath));
    }

    [Fact]
    public async Task FileWrite_PathTraversal_ReturnsFailure()
    {
        var keyword = new FileWriteKeyword();
        var args = new FileWriteArgs { Path = "../../../tmp/malicious.txt", Content = "hack", BasePath = _testDir };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    #endregion

    #region FileDelete Tests

    [Fact]
    public async Task FileDelete_ExistingFile_DeletesFile()
    {
        var deleteFile = Path.Join(_testDir, "to_delete.txt");
        File.WriteAllText(deleteFile, "delete me");

        var keyword = new FileDeleteKeyword();
        var args = new FileDeleteArgs { Path = "to_delete.txt", BasePath = _testDir };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
        Assert.False(File.Exists(deleteFile));
    }

    [Fact]
    public async Task FileDelete_PathTraversal_ReturnsFailure()
    {
        var keyword = new FileDeleteKeyword();
        var args = new FileDeleteArgs { Path = "../../../etc/passwd", BasePath = _testDir };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    #endregion

    #region ExcelRead Tests

    [Fact]
    public async Task ExcelRead_ValidWorkbook_ReturnsRowsFromFirstWorksheet()
    {
        var workbookPath = Path.Join(_testDir, "employees.xlsx");
        CreateWorkbook(
            workbookPath,
            [
                ["First Name", "Last Name ", "Phone Number"],
                ["John", "Smith", "40716543298"],
                ["Jane", "Dorsey", "40791345621"],
                ["", "", ""]
            ]);

        var keyword = new ExcelReadKeyword();
        var args = new ExcelReadArgs { Path = "employees.xlsx", BasePath = _testDir };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);

        var outputs = result.Outputs!;
        var count = (int)outputs.GetType().GetProperty("count")!.GetValue(outputs)!;
        var rows = (System.Collections.IEnumerable)outputs.GetType().GetProperty("rows")!.GetValue(outputs)!;
        var firstRow = Assert.IsAssignableFrom<Dictionary<string, object?>>(rows.Cast<object>().First());

        Assert.Equal(2, count);
        Assert.Equal("John", firstRow["First Name"]);
        Assert.Equal("Smith", firstRow["Last Name"]);
        Assert.Equal("40716543298", firstRow["Phone Number"]);
    }

    [Fact]
    public async Task ExcelRead_PathTraversal_ReturnsFailure()
    {
        var keyword = new ExcelReadKeyword();
        var args = new ExcelReadArgs { Path = "../../../secret.xlsx", BasePath = _testDir };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    #endregion

    private static void CreateWorkbook(string workbookPath, string[][] rows)
    {
        var sharedStrings = rows
            .SelectMany(row => row)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        using var archive = ZipFile.Open(workbookPath, ZipArchiveMode.Create);
        AddEntry(
            archive,
            "xl/workbook.xml",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                      xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <sheets>
                <sheet name="Sheet1" sheetId="1" r:id="rId1" />
              </sheets>
            </workbook>
            """);
        AddEntry(
            archive,
            "xl/_rels/workbook.xml.rels",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1"
                            Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"
                            Target="worksheets/sheet1.xml" />
            </Relationships>
            """);
        AddEntry(
            archive,
            "xl/sharedStrings.xml",
            $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
                 count="{sharedStrings.Count}"
                 uniqueCount="{sharedStrings.Count}">
            {string.Join(Environment.NewLine, sharedStrings.Select(value => $"  <si><t>{System.Security.SecurityElement.Escape(value)}</t></si>"))}
            </sst>
            """);
        AddEntry(
            archive,
            "xl/worksheets/sheet1.xml",
            $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetData>
            {string.Join(Environment.NewLine, rows.Select((row, rowIndex) => CreateRowXml(row, rowIndex + 1, sharedStrings)))}
              </sheetData>
            </worksheet>
            """);
    }

    private static string CreateRowXml(string[] row, int rowIndex, List<string> sharedStrings)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append($"    <row r=\"{rowIndex}\">");

        foreach (var (value, columnIndex) in row.Select((item, index) => (item, index)))
        {
            var cellReference = $"{GetColumnName(columnIndex)}{rowIndex}";
            var sharedIndex = sharedStrings.IndexOf(value);
            builder.Append($"<c r=\"{cellReference}\" t=\"s\"><v>{sharedIndex}</v></c>");
        }

        builder.Append("</row>");
        return builder.ToString();
    }

    private static string GetColumnName(int columnIndex)
    {
        var value = columnIndex + 1;
        var chars = new Stack<char>();

        while (value > 0)
        {
            value--;
            chars.Push((char)('A' + (value % 26)));
            value /= 26;
        }

        return new string(chars.ToArray());
    }

    private static void AddEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
