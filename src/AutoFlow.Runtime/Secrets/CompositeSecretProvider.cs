// =============================================================================
// CompositeSecretProvider.cs — композитный провайдер секретов.
//
// Объединяет несколько ISecretProvider и пробует каждый по очереди.
// Позволяет использовать разные источники секретов одновременно.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFlow.Abstractions;

namespace AutoFlow.Runtime.Secrets;

/// <summary>
/// Композитный провайдер секретов. Пробует несколько провайдеров по очереди.
/// </summary>
public sealed class CompositeSecretProvider : ISecretProvider
{
    private readonly List<ISecretProvider> _providers;

    public CompositeSecretProvider(IEnumerable<ISecretProvider> providers)
    {
        _providers = providers?.ToList() ?? new List<ISecretProvider>();
    }

    public async Task<string?> ResolveAsync(string secretRef, CancellationToken cancellationToken = default)
    {
        foreach (var provider in _providers.Where(p => p.CanResolve(secretRef)))
        {
            var value = await provider.ResolveAsync(secretRef, cancellationToken).ConfigureAwait(false);
            if (value is not null)
                return value;
        }

        return null;
    }

    public bool CanResolve(string secretRef)
    {
        return _providers.Any(p => p.CanResolve(secretRef));
    }

    /// <summary>Добавляет провайдер в цепочку.</summary>
    public void AddProvider(ISecretProvider provider)
    {
        _providers.Add(provider ?? throw new ArgumentNullException(nameof(provider)));
    }

    public int Count => _providers.Count;
    public IReadOnlyList<ISecretProvider> Providers => _providers;
}
