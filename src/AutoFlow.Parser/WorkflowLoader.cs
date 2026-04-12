// Этот код нужен для загрузки workflow с поддержкой импортов.
using System;
using System.Collections.Generic;
using System.IO;
using AutoFlow.Abstractions;

namespace AutoFlow.Parser;

public sealed class WorkflowLoader
{
    private readonly IWorkflowParser _parser;
    private readonly HashSet<string> _loadedFiles = new(StringComparer.OrdinalIgnoreCase);

    public WorkflowLoader(IWorkflowParser parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    public WorkflowDocument LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Путь к файлу не может быть пустым.", nameof(filePath));

        var absolutePath = Path.GetFullPath(filePath);
        
        if (!File.Exists(absolutePath))
            throw new FileNotFoundException($"Workflow файл не найден: {absolutePath}");

        _loadedFiles.Clear();
        return LoadWithImports(absolutePath);
    }

    public WorkflowDocument LoadFromString(string yamlContent, string? basePath = null)
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
            throw new ArgumentException("YAML содержимое не может быть пустым.", nameof(yamlContent));

        var document = _parser.Parse(yamlContent);

        if (document.Imports.Count == 0)
            return document;

        if (string.IsNullOrWhiteSpace(basePath))
            return document;

        _loadedFiles.Clear();
        return MergeWithImports(document, basePath);
    }

    private WorkflowDocument LoadWithImports(string filePath)
    {
        var absolutePath = Path.GetFullPath(filePath);
        var normalizedPath = NormalizePath(absolutePath);

        if (_loadedFiles.Contains(normalizedPath))
            throw new InvalidOperationException($"Обнаружен циклический импорт: {normalizedPath}");

        _loadedFiles.Add(normalizedPath);

        var yaml = File.ReadAllText(absolutePath);
        var document = _parser.Parse(yaml);

        var directory = Path.GetDirectoryName(absolutePath) ?? Directory.GetCurrentDirectory();

        return MergeWithImports(document, directory);
    }

    private WorkflowDocument MergeWithImports(WorkflowDocument mainDocument, string baseDirectory)
    {
        if (mainDocument.Imports.Count == 0)
            return mainDocument;

        var mergedVariables = new Dictionary<string, object?>(mainDocument.Variables);
        var mergedTasks = new Dictionary<string, TaskNode>(mainDocument.Tasks);

        foreach (var importPath in mainDocument.Imports)
        {
            var resolvedPath = ResolveImportPath(importPath, baseDirectory);
            var importedDocument = LoadWithImports(resolvedPath);

            MergeVariables(mergedVariables, importedDocument.Variables, importPath);
            MergeTasks(mergedTasks, importedDocument.Tasks, importPath);
        }

        return new WorkflowDocument
        {
            SchemaVersion = mainDocument.SchemaVersion,
            Name = mainDocument.Name,
            FilePath = mainDocument.FilePath,
            Imports = mainDocument.Imports,
            Variables = mergedVariables,
            Tasks = mergedTasks
        };
    }

    private string ResolveImportPath(string importPath, string baseDirectory)
    {
        // Поддержка разных форматов путей:
        // - ./relative/path.yaml
        // - ../parent/path.yaml
        // - relative/path.yaml
        // - /absolute/path.yaml (или C:\absolute\path.yaml на Windows)

        if (Path.IsPathRooted(importPath))
            return importPath;

        return Path.GetFullPath(Path.Combine(baseDirectory, importPath));
    }

    private static void MergeVariables(
        Dictionary<string, object?> target,
        Dictionary<string, object?> source,
        string importPath)
    {
        foreach (var kvp in source)
        {
            if (target.ContainsKey(kvp.Key))
            {
                // Переменные из основного файла имеют приоритет
                // Можно добавить предупреждение или ошибку при конфликте
                continue;
            }

            target[kvp.Key] = kvp.Value;
        }
    }

    private static void MergeTasks(
        Dictionary<string, TaskNode> target,
        Dictionary<string, TaskNode> source,
        string importPath)
    {
        foreach (var kvp in source)
        {
            if (target.ContainsKey(kvp.Key))
            {
                throw new InvalidOperationException(
                    $"Конфликт имён задач: задача '{kvp.Key}' уже определена. " +
                    $"Импорт: {importPath}");
            }

            target[kvp.Key] = kvp.Value;
        }
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).Replace('\\', '/').ToLowerInvariant();
    }
}
