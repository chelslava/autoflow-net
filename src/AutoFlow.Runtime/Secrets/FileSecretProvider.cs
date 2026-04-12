// =============================================================================
// FileSecretProvider.cs — провайдер секретов из файлов.
//
// Разрешает ссылки вида: file:/path/to/secret.txt
// Полезно для Docker secrets и Kubernetes secrets (примонтированные файлы).
// =============================================================================

using System;
using System.IO;
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
    public Task<string?> ResolveAsync(string secretRef, CancellationToken cancellationToken = default)
    {
        var filePath = ExtractFilePath(secretRef);
        if (filePath is null)
            return Task.FromResult<string?>(null);

        if (!File.Exists(filePath))
            return Task.FromResult<string?>(null);

        try
        {
            var content = File.ReadAllText(filePath).Trim();
            return Task.FromResult<string?>(content);
        }
        catch (IOException)
        {
            return Task.FromResult<string?>(null);
        }
    }

    public bool CanResolve(string secretRef)
    {
        return ExtractFilePath(secretRef) is not null;
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
