// =============================================================================
// ISecretProvider.cs — интерфейс для разрешения секретов.
//
// Поддерживает различные источники секретов:
// - Переменные окружения (env:NAME)
// - Файлы (file:/path/to/secret)
// - Vault (vault:path/to/secret)
//
// Секреты маскируются в логах и отчётах через SecretMasker.
// =============================================================================

using System.Threading;
using System.Threading.Tasks;

namespace AutoFlow.Abstractions;

/// <summary>
/// Интерфейс для разрешения секретов из различных источников.
/// Реализации регистрируются через DI: services.AddSingleton&lt;ISecretProvider, EnvSecretProvider&gt;()
/// </summary>
public interface ISecretProvider
{
    /// <summary>
    /// Разрешает секрет по ссылке.
    /// </summary>
    /// <param name="secretRef">Ссылка на секрет (env:NAME, vault:path, file:/path).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Значение секрета или null, если не найден.</returns>
    Task<string?> ResolveAsync(string secretRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет, может ли провайдер обработать данную ссылку.
    /// </summary>
    /// <param name="secretRef">Ссылка на секрет.</param>
    /// <returns>True, если провайдер поддерживает этот формат ссылки.</returns>
    bool CanResolve(string secretRef);
}
