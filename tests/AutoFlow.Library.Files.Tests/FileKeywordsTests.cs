using System;
using System.IO;
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
    private readonly string _testFile;
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IExecutionContext> _executionContextMock;

    public FileKeywordsTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"autoflow_file_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _testFile = Path.Combine(_testDir, "test.txt");
        File.WriteAllText(_testFile, "Hello, AutoFlow!");

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

    #region FileReadKeyword Tests

    [Fact]
    public async Task FileRead_ExistingFile_ReturnsContent()
    {
        var keyword = new FileReadKeyword();
        var args = new FileReadArgs { Path = _testFile };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello, AutoFlow!", result.Outputs);
    }

    [Fact]
    public async Task FileRead_NonExistingFile_ReturnsFailure()
    {
        var keyword = new FileReadKeyword();
        var args = new FileReadArgs { Path = Path.Combine(_testDir, "nonexistent.txt") };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FileRead_PathTraversal_ReturnsFailure()
    {
        var keyword = new FileReadKeyword();
        var args = new FileReadArgs { Path = "../../../etc/passwd" };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task FileRead_EmptyPath_ReturnsFailure()
    {
        var keyword = new FileReadKeyword();
        var args = new FileReadArgs { Path = "" };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    #endregion

    #region FileWriteKeyword Tests

    [Fact]
    public async Task FileWrite_NewFile_CreatesFile()
    {
        var keyword = new FileWriteKeyword();
        var newFile = Path.Combine(_testDir, "new.txt");
        var args = new FileWriteArgs { Path = newFile, Content = "New content" };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(newFile));
        Assert.Equal("New content", File.ReadAllText(newFile));
    }

    [Fact]
    public async Task FileWrite_ExistingFile_OverwritesContent()
    {
        var keyword = new FileWriteKeyword();
        var args = new FileWriteArgs { Path = _testFile, Content = "Overwritten" };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
        Assert.Equal("Overwritten", File.ReadAllText(_testFile));
    }

    [Fact]
    public async Task FileWrite_PathTraversal_ReturnsFailure()
    {
        var keyword = new FileWriteKeyword();
        var args = new FileWriteArgs { Path = "../../../tmp/malicious.txt", Content = "hack" };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task FileWrite_CreatesDirectoryIfNotExists()
    {
        var keyword = new FileWriteKeyword();
        var nestedFile = Path.Combine(_testDir, "nested", "dir", "file.txt");
        var args = new FileWriteArgs { Path = nestedFile, Content = "nested content" };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(nestedFile));
    }

    #endregion

    #region FileExistsKeyword Tests

    [Fact]
    public async Task FileExists_ExistingFile_ReturnsTrue()
    {
        var keyword = new FileExistsKeyword();
        var args = new FileExistsArgs { Path = _testFile };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
        Assert.True((bool)result.Outputs!);
    }

    [Fact]
    public async Task FileExists_NonExistingFile_ReturnsFalse()
    {
        var keyword = new FileExistsKeyword();
        var args = new FileExistsArgs { Path = Path.Combine(_testDir, "nonexistent.txt") };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
        Assert.False((bool)result.Outputs!);
    }

    [Fact]
    public async Task FileExists_Directory_ReturnsFalse()
    {
        var keyword = new FileExistsKeyword();
        var args = new FileExistsArgs { Path = _testDir };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
        Assert.False((bool)result.Outputs!);
    }

    [Fact]
    public async Task FileExists_PathTraversal_ReturnsFailure()
    {
        var keyword = new FileExistsKeyword();
        var args = new FileExistsArgs { Path = "../../../etc/passwd" };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    #endregion

    #region FileDeleteKeyword Tests

    [Fact]
    public async Task FileDelete_ExistingFile_DeletesFile()
    {
        var deleteFile = Path.Combine(_testDir, "to_delete.txt");
        File.WriteAllText(deleteFile, "delete me");

        var keyword = new FileDeleteKeyword();
        var args = new FileDeleteArgs { Path = deleteFile };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.True(result.IsSuccess);
        Assert.False(File.Exists(deleteFile));
    }

    [Fact]
    public async Task FileDelete_NonExistingFile_ReturnsFailure()
    {
        var keyword = new FileDeleteKeyword();
        var args = new FileDeleteArgs { Path = Path.Combine(_testDir, "nonexistent.txt") };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task FileDelete_PathTraversal_ReturnsFailure()
    {
        var keyword = new FileDeleteKeyword();
        var args = new FileDeleteArgs { Path = "../../../etc/passwd" };

        var result = await keyword.ExecuteAsync(CreateContext(), args);

        Assert.False(result.IsSuccess);
    }

    #endregion
}
