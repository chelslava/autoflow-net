// =============================================================================
// FileSecretProvider.cs — провайдер секретов из файлов.
//
// Разрешает ссылки вида: file:/path/to/secret.txt
// Полезно для Docker secrets и Kubernetes secrets (примонтированные файлы).
//
// БЕЗОПАСНОСТЬ: Поддерживается whitelist разрешённых директорий.
// По умолчанию: /run/secrets (Docker), /var/run/secrets (Kubernetes), ./secrets
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;

namespace AutoFlow.Runtime.Secrets;

/// <summary>
/// Провайдер секретов из файлов.
/// Поддерживает ссылки вида: file:/path/to/secret.txt
/// Полезно для Docker secrets (/run/secrets/...) и Kubernetes secrets.
/// </summary>
public sealed class FileSecretProvider : ISecretProvider
{
    private readonly HashSet<string> _allowedDirectories;
    private readonly bool _allowAnyPath;

    /// <summary>
    /// Создаёт провайдер с whitelist разрешённых директорий.
    /// </summary>
    /// <param name="allowedDirectories">
    /// Список разрешённых директорий. Если null или пустой, используются defaults:
    /// - /run/secrets (Docker secrets)
    /// - /var/run/secrets (Kubernetes secrets)
    /// - ./secrets (локальная директория secrets относительно рабочей директории)
    /// </param>
    /// <param name="allowAnyPath">
    /// Если true, разрешает любой путь (НЕ БЕЗОПАСНО - используйте только для тестов!).
    /// </param>
    public FileSecretProvider(
        IEnumerable<string>? allowedDirectories = null,
        bool allowAnyPath = false)
    {
        _allowAnyPath = allowAnyPath;

        if (allowAnyPath)
        {
            _allowedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var dirs = allowedDirectories?.ToList() ?? GetDefaultAllowedDirectories();
        _allowedDirectories = new HashSet<string>(
            dirs.Select(d => NormalizeDirectory(d)).Where(d => d is not null)!,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Директории по умолчанию, в которых разрешено искать секреты.
    /// </summary>
    private static List<string> GetDefaultAllowedDirectories()
    {
        var defaults = new List<string>();

        // Docker secrets
        if (Directory.Exists("/run/secrets"))
            defaults.Add("/run/secrets");

        // Kubernetes secrets
        if (Directory.Exists("/var/run/secrets"))
            defaults.Add("/var/run/secrets");

        // Локальная директория secrets
        var localSecrets = Path.Join(Directory.GetCurrentDirectory(), "secrets");
        defaults.Add(localSecrets);

        return defaults;
    }

    private static string? NormalizeDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var fullPath = Path.GetFullPath(path);
            // Ensure trailing separator for proper prefix matching
            return fullPath.EndsWith(Path.DirectorySeparatorChar)
                ? fullPath
                : fullPath + Path.DirectorySeparatorChar;
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
        {
            return null;
        }
    }

    public Task<string?> ResolveAsync(string secretRef, CancellationToken cancellationToken = default)
    {
        var filePath = ExtractFilePath(secretRef);
        if (filePath is null)
            return Task.FromResult<string?>(null);

        if (!IsPathAllowed(filePath))
            return Task.FromResult<string?>(null);

        if (!File.Exists(filePath))
            return Task.FromResult<string?>(null);

        try
        {
            // Limit file size to prevent OOM
            const long maxFileSizeBytes = 64 * 1024; // 64 KB max for secret files
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > maxFileSizeBytes)
            {
                return Task.FromResult<string?>(null);
            }

            var content = File.ReadAllText(filePath).Trim();
            return Task.FromResult<string?>(content);
        }
        catch (IOException)
        {
            return Task.FromResult<string?>(null);
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult<string?>(null);
        }
    }

    public bool CanResolve(string secretRef)
    {
        var filePath = ExtractFilePath(secretRef);
        return filePath is not null && IsPathAllowed(filePath);
    }

    private bool IsPathAllowed(string filePath)
    {
        if (_allowAnyPath)
            return true;

        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        // Reject suspicious patterns
        if (filePath.Contains("..") || filePath.Contains("~"))
            return false;

        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var normalizedPath = fullPath.EndsWith(Path.DirectorySeparatorChar)
                ? fullPath
                : fullPath + Path.DirectorySeparatorChar;

            // Check if path is within any allowed directory
            foreach (var allowedDir in _allowedDirectories)
            {
                if (fullPath.StartsWith(allowedDir, StringComparison.OrdinalIgnoreCase) ||
                    normalizedPath.StartsWith(allowedDir, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
        {
            return false;
        }
    }

    private static string? ExtractFilePath(string secretRef)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
            return null;

        var normalized = secretRef.Trim();

        // Убираем ${ } если есть
        if (normalized.StartsWith("${") && normalized.EndsWith("}"))
            normalized = normalized[2..^1].Trim();

        // Проверяем префикс file:
        if (!normalized.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            return null;

        return normalized[5..].Trim();
    }
}
