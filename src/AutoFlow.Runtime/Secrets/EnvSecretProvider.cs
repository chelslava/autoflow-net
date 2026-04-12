// =============================================================================
// EnvSecretProvider.cs — провайдер секретов из переменных окружения.
//
// Разрешает ссылки вида: env:NAME, ${env:NAME}
// Пример: secret: env:DATABASE_PASSWORD → значение из Environment.GetEnvironmentVariable
// =============================================================================

using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;

namespace AutoFlow.Runtime.Secrets;

/// <summary>
/// Провайдер секретов из переменных окружения.
/// Поддерживает ссылки вида: env:NAME, ${env:NAME}
/// </summary>
public sealed class EnvSecretProvider : ISecretProvider
{
    public Task<string?> ResolveAsync(string secretRef, CancellationToken cancellationToken = default)
    {
        var envName = ExtractEnvName(secretRef);
        if (envName is null)
            return Task.FromResult<string?>(null);

        var value = Environment.GetEnvironmentVariable(envName);
        return Task.FromResult(value);
    }

    public bool CanResolve(string secretRef)
    {
        return ExtractEnvName(secretRef) is not null;
    }

    private static string? ExtractEnvName(string secretRef)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
            return null;

        // Формат: env:NAME или ${env:NAME}
        var normalized = secretRef.Trim();

        // Убираем ${ } если есть
        if (normalized.StartsWith("${") && normalized.EndsWith("}"))
            normalized = normalized[2..^1].Trim();

        // Проверяем префикс env:
        if (!normalized.StartsWith("env:", StringComparison.OrdinalIgnoreCase))
            return null;

        return normalized[4..].Trim();
    }
}
