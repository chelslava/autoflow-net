using System;
using System.IO;
using AutoFlow.Library.Files;
using Xunit;

namespace AutoFlow.Library.Files.Tests;

public sealed class PathValidatorTests : IDisposable
{
    private readonly string _testBasePath;
    private readonly string _testSubDir;
    private readonly string _testFile;

    public PathValidatorTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), $"autoflow_test_{Guid.NewGuid():N}");
        _testSubDir = Path.Combine(_testBasePath, "subdir");
        _testFile = Path.Combine(_testBasePath, "test.txt");

        Directory.CreateDirectory(_testSubDir);
        File.WriteAllText(_testFile, "test content");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, recursive: true);
        }
    }

    [Fact]
    public void ValidatePath_ValidRelativePath_ReturnsValid()
    {
        var result = PathValidator.ValidatePath("test.txt", _testBasePath);

        Assert.True(result.IsValid);
        Assert.Equal(_testFile, result.FullPath);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidatePath_ValidSubdirectoryPath_ReturnsValid()
    {
        var result = PathValidator.ValidatePath("subdir/test.txt", _testBasePath);

        Assert.True(result.IsValid);
        Assert.Contains("subdir", result.FullPath);
    }

    [Fact]
    public void ValidatePath_EmptyPath_ReturnsInvalid()
    {
        var result = PathValidator.ValidatePath("", _testBasePath);

        Assert.False(result.IsValid);
        Assert.Null(result.FullPath);
        Assert.Contains("empty", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidatePath_NullPath_ReturnsInvalid()
    {
        var result = PathValidator.ValidatePath(null!, _testBasePath);

        Assert.False(result.IsValid);
        Assert.Null(result.FullPath);
    }

    [Fact]
    public void ValidatePath_PathTraversalWithDoubleDot_ReturnsInvalid()
    {
        var result = PathValidator.ValidatePath("../../../etc/passwd", _testBasePath);

        Assert.False(result.IsValid);
        Assert.Null(result.FullPath);
        Assert.Contains("outside", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidatePath_PathTraversalWithBackslashes_ReturnsInvalid()
    {
        var result = PathValidator.ValidatePath("..\\..\\windows\\system32", _testBasePath);

        Assert.False(result.IsValid);
        Assert.Null(result.FullPath);
    }

    [Fact]
    public void ValidatePath_AbsolutePathOutsideBase_ReturnsInvalid()
    {
        var result = PathValidator.ValidatePath("/etc/passwd", _testBasePath);

        Assert.False(result.IsValid);
        Assert.Null(result.FullPath);
    }

    [Fact]
    public void ValidatePath_AbsolutePathInsideBase_ReturnsValid()
    {
        var result = PathValidator.ValidatePath(_testFile, _testBasePath);

        Assert.True(result.IsValid);
        Assert.Equal(_testFile, result.FullPath);
    }

    [Fact]
    public void ValidatePath_PathWithTilde_ReturnsInvalid()
    {
        var result = PathValidator.ValidatePath("~/secret.txt", _testBasePath);

        Assert.False(result.IsValid);
        Assert.Null(result.FullPath);
        Assert.Contains("suspicious", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidatePath_DoubleSlashInPath_ReturnsInvalid()
    {
        var result = PathValidator.ValidatePath("test//file.txt", _testBasePath);

        Assert.False(result.IsValid);
        Assert.Contains("suspicious", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidatePath_NoBasePath_UsesCurrentDirectory()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var result = PathValidator.ValidatePath("somefile.txt", basePath: null);

        Assert.True(result.IsValid);
        Assert.StartsWith(currentDir, result.FullPath);
    }

    [Fact]
    public void GetAllowedBasePath_ValidPath_ReturnsFullPath()
    {
        var result = PathValidator.GetAllowedBasePath(_testBasePath);

        Assert.Equal(_testBasePath, result);
    }
}
