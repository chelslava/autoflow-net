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
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IExecutionContext> _executionContextMock;

    public FileKeywordsTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"autoflow_file_test_{Guid.NewGuid():N}");
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
        var testFile = Path.Combine(_testDir, "test.txt");
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
        var fullPath = Path.Combine(_testDir, "new.txt");
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
        var deleteFile = Path.Combine(_testDir, "to_delete.txt");
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
}
