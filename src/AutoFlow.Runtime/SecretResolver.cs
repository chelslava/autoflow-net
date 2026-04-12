// =============================================================================
// SecretResolver.cs — разрешение секретов в переменных и аргументах.
//
// Заменяет ссылки вида ${secret:ref} на реальные значения из ISecretProvider.
// Интегрируется с VariableResolver и маскирует секреты через SecretMasker.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;
using AutoFlow.Runtime.Secrets;

namespace AutoFlow.Runtime;

/// <summary>
/// Разрешает секреты в строках. Заменяет ${secret:ref} на значения из провайдеров.
/// </summary>
public sealed class SecretResolver
{
    private static readonly Regex SecretRegex = new(
        @"\$\{secret:(?<ref>[^}]+)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly CompositeSecretProvider _provider;
    private readonly SecretMasker _masker;

    public SecretResolver(IEnumerable<ISecretProvider> providers, SecretMasker masker)
    {
        _provider = new CompositeSecretProvider(providers ?? Enumerable.Empty<ISecretProvider>());
        _masker = masker ?? throw new ArgumentNullException(nameof(masker));
    }

    /// <summary>
    /// Разрешает секреты в строке, заменяя ${secret:ref} на значения.
    /// </summary>
    public async Task<string> ResolveAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        var matches = SecretRegex.Matches(input);
        if (matches.Count == 0)
            return input;

        var result = input;
        var resolvedSecrets = new List<string>();

        foreach (Match match in matches)
        {
            var secretRef = match.Groups["ref"].Value;
            var secretValue = await _provider.ResolveAsync(secretRef, cancellationToken).ConfigureAwait(false);

            if (secretValue is not null)
            {
                result = result.Replace(match.Value, secretValue, StringComparison.Ordinal);
                resolvedSecrets.Add(secretValue);
            }
        }

        // Регистрируем секреты для маскирования
        _masker.RegisterSecrets(resolvedSecrets);

        return result;
    }

    /// <summary>
    /// Разрешает секреты в объекте (рекурсивно для словарей и списков).
    /// </summary>
    public async Task<object?> ResolveObjectAsync(object? value, CancellationToken cancellationToken = default)
    {
        return value switch
        {
            null => null,
            string s => await ResolveAsync(s, cancellationToken).ConfigureAwait(false),
            IDictionary<string, object?> dict => await ResolveDictionaryAsync(dict, cancellationToken).ConfigureAwait(false),
            System.Collections.IEnumerable enumerable when value is not string => await ResolveEnumerableAsync(enumerable, cancellationToken).ConfigureAwait(false),
            _ => value
        };
    }

    private async Task<Dictionary<string, object?>> ResolveDictionaryAsync(
        IDictionary<string, object?> dictionary,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, object?>();

        foreach (var kvp in dictionary)
        {
            result[kvp.Key] = await ResolveObjectAsync(kvp.Value, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    private async Task<List<object?>> ResolveEnumerableAsync(
        System.Collections.IEnumerable enumerable,
        CancellationToken cancellationToken)
    {
        var result = new List<object?>();

        foreach (var item in enumerable)
        {
            var resolved = await ResolveObjectAsync(item, cancellationToken).ConfigureAwait(false);
            result.Add(resolved);
        }

        return result;
    }

    /// <summary>Возвращает SecretMasker для использования в логах и отчётах.</summary>
    public SecretMasker GetMasker() => _masker;
}
