using System;
using System.IO;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using AutoFlow.Parser;
using Xunit;

namespace AutoFlow.Parser.Tests;

public sealed class WorkflowLoaderTests
{
    private readonly IWorkflowParser _parser;
    private readonly WorkflowLoader _loader;

    public WorkflowLoaderTests()
    {
        _parser = new YamlWorkflowParser();
        _loader = new WorkflowLoader(_parser);
    }

    [Fact]
    public void Constructor_NullParser_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new WorkflowLoader(null!));
    }

    [Fact]
    public void LoadFromFile_NullFilePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _loader.LoadFromFile(null!));
    }

    [Fact]
    public void LoadFromFile_EmptyFilePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _loader.LoadFromFile(string.Empty));
    }

    [Fact]
    public void LoadFromFile_WhitespaceFilePath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _loader.LoadFromFile("   "));
    }

    [Fact]
    public void LoadFromFile_NonexistentFile_ThrowsFileNotFoundException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.yaml");

        Assert.Throws<FileNotFoundException>(() =>
            _loader.LoadFromFile(tempFile));
    }

    [Fact]
    public async Task LoadFromFile_SimpleWorkflow_LoadsSuccessfully()
    {
        var yaml = @"
schema_version: 1
name: test_workflow

tasks:
  main:
    steps:
      - step:
          id: step1
          uses: log.info
          with:
            message: Hello
";

        var tempFile = await WriteTempFileAsync(yaml);

        try
        {
            var document = _loader.LoadFromFile(tempFile);

            Assert.Equal("test_workflow", document.Name);
            Assert.Single(document.Tasks);
            Assert.True(document.Tasks.ContainsKey("main"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadFromFile_WorkflowWithVariables_ParsesVariables()
    {
        var yaml = @"
schema_version: 1
name: test_workflow

variables:
  api_url: https://api.example.com
  timeout: 30

tasks:
  main:
    steps:
      - step:
          id: step1
          uses: log.info
          with:
            message: ${api_url}
";

        var tempFile = await WriteTempFileAsync(yaml);

        try
        {
            var document = _loader.LoadFromFile(tempFile);

            Assert.Equal(2, document.Variables.Count);
            Assert.Equal("https://api.example.com", document.Variables["api_url"]);
            Assert.Equal(30, document.Variables["timeout"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadFromFile_WorkflowWithImports_ImportsSuccessfully()
    {
        var baseDir = Path.GetTempPath();
        var mainFile = Path.Combine(baseDir, $"main_{Guid.NewGuid()}.yaml");
        var importFile = Path.Combine(baseDir, $"imported_{Guid.NewGuid()}.yaml");

        var importYaml = $@"
schema_version: 1
name: imported_workflow

tasks:
  imported_task:
    steps:
      - step:
          id: step1
          uses: log.info
          with:
            message: Imported
";

        var mainYaml = $@"
schema_version: 1
name: main_workflow

imports:
  - {Path.GetFileName(importFile)}

tasks:
  main:
    steps:
      - step:
          id: step1
          uses: log.info
          with:
            message: Main
";

        try
        {
            await File.WriteAllTextAsync(importFile, importYaml);
            await File.WriteAllTextAsync(mainFile, mainYaml);

            var document = _loader.LoadFromFile(mainFile);

            Assert.Equal(2, document.Tasks.Count);
            Assert.True(document.Tasks.ContainsKey("main"));
            Assert.True(document.Tasks.ContainsKey("imported_task"));
        }
        finally
        {
            if (File.Exists(mainFile)) File.Delete(mainFile);
            if (File.Exists(importFile)) File.Delete(importFile);
        }
    }

    [Fact]
    public async Task LoadFromFile_ImportPathTraversal_ThrowsInvalidOperationException()
    {
        var baseDir = Path.GetTempPath();
        var mainFile = Path.Combine(baseDir, $"main_{Guid.NewGuid()}.yaml");
        var targetFile = Path.Combine(baseDir, $"target_{Guid.NewGuid()}.yaml");

        await File.WriteAllTextAsync(targetFile, "secret: password123");

        var mainYaml = $@"
schema_version: 1
name: test

imports:
  - ../{Path.GetFileName(targetFile)}

tasks:
  main:
    steps: []
";

        try
        {
            await File.WriteAllTextAsync(mainFile, mainYaml);

            Assert.Throws<InvalidOperationException>(() =>
                _loader.LoadFromFile(mainFile));
        }
        finally
        {
            if (File.Exists(mainFile)) File.Delete(mainFile);
            if (File.Exists(targetFile)) File.Delete(targetFile);
        }
    }

    [Fact]
    public async Task LoadFromFile_AbsoluteImportPath_ThrowsInvalidOperationException()
    {
        var mainFile = Path.GetTempFileName();

        var mainYaml = $@"
schema_version: 1
name: test

imports:
  - {Path.GetFullPath(mainFile)}

tasks:
  main:
    steps: []
";

        try
        {
            await File.WriteAllTextAsync(mainFile, mainYaml);

            Assert.Throws<InvalidOperationException>(() =>
                _loader.LoadFromFile(mainFile));
        }
        finally
        {
            if (File.Exists(mainFile)) File.Delete(mainFile);
        }
    }

    [Fact]
    public async Task LoadFromString_SimpleWorkflow_LoadsSuccessfully()
    {
        var yaml = @"
schema_version: 1
name: test_workflow

tasks:
  main:
    steps:
      - step:
          id: step1
          uses: log.info
          with:
            message: Hello
";

        var document = _loader.LoadFromString(yaml);

        Assert.Equal("test_workflow", document.Name);
        Assert.Single(document.Tasks);
    }

    [Fact]
    public async Task LoadFromString_EmptyYaml_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _loader.LoadFromString(null!));
    }

    [Fact]
    public async Task LoadFromString_WhitespaceYaml_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            _loader.LoadFromString("   "));
    }

    [Fact]
    public async Task LoadFromString_WorkflowWithImports_RequiresBasePath()
    {
        var yaml = @"
schema_version: 1
name: test

imports:
  - imported.yaml

tasks:
  main:
    steps: []
";

        var document = _loader.LoadFromString(yaml);

        Assert.Single(document.Tasks);
    }

    [Fact]
    public async Task LoadFromString_WorkflowWithImports_WithBasePath_ResolvesImports()
    {
        var baseDir = Path.GetTempPath();
        var importFile = Path.Combine(baseDir, $"imported_{Guid.NewGuid()}.yaml");

        var importYaml = $@"
schema_version: 1
name: imported_workflow

tasks:
  imported_task:
    steps:
      - step:
          id: step1
          uses: log.info
          with:
            message: Imported
";

        var mainYaml = $@"
schema_version: 1
name: test

imports:
  - {Path.GetFileName(importFile)}

tasks:
  main:
    steps: []
";

        try
        {
            var importFullPath = Path.Combine(baseDir, Path.GetFileName(importFile));
            await File.WriteAllTextAsync(importFile, importYaml);

            var document = _loader.LoadFromString(mainYaml, baseDir);

            Assert.Equal(2, document.Tasks.Count);
            Assert.Contains("main", document.Tasks.Keys);
            Assert.Contains("imported_task", document.Tasks.Keys);
        }
        finally
        {
            if (File.Exists(importFile)) File.Delete(importFile);
        }
    }

    [Fact]
    public async Task LoadFromFile_CyclicImport_ThrowsInvalidOperationException()
    {
        var baseDir = Path.GetTempPath();
        var file1 = Path.Combine(baseDir, $"file1_{Guid.NewGuid()}.yaml");
        var file2 = Path.Combine(baseDir, $"file2_{Guid.NewGuid()}.yaml");

        var yaml1 = $@"
schema_version: 1
name: file1

imports:
  - {Path.GetFileName(file2)}

tasks:
  main:
    steps: []
";

        var yaml2 = $@"
schema_version: 1
name: file2

imports:
  - {Path.GetFileName(file1)}

tasks:
  main:
    steps: []
";

        try
        {
            await File.WriteAllTextAsync(file1, yaml1);
            await File.WriteAllTextAsync(file2, yaml2);

            Assert.Throws<InvalidOperationException>(() =>
                _loader.LoadFromFile(file1));
        }
        finally
        {
            if (File.Exists(file1)) File.Delete(file1);
            if (File.Exists(file2)) File.Delete(file2);
        }
    }

    [Theory]
    [InlineData("~/path.yaml")]
    [InlineData("~")]
    public async Task LoadFromFile_HomeDirectoryPath_ThrowsInvalidOperationException(string importPath)
    {
        var mainFile = Path.GetTempFileName();

        var mainYaml = $@"
schema_version: 1
name: test

imports:
  - {importPath}

tasks:
  main:
    steps: []
";

        try
        {
            await File.WriteAllTextAsync(mainFile, mainYaml);

            Assert.Throws<InvalidOperationException>(() =>
                _loader.LoadFromFile(mainFile));
        }
        finally
        {
            if (File.Exists(mainFile)) File.Delete(mainFile);
        }
    }

    private static async Task<string> WriteTempFileAsync(string content)
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, content);
        return tempFile;
    }
}
