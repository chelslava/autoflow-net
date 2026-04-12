using System;
using System.Collections.Generic;
using System.Linq;
using AutoFlow.Abstractions;

namespace AutoFlow.Runtime;

public sealed class KeywordRegistry : IKeywordMetadataProvider
{
    private readonly Dictionary<string, KeywordRegistration> _registrations =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, Type handlerType, Type argsType, string? category = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя keyword не может быть пустым.", nameof(name));

        if (_registrations.ContainsKey(name))
            throw new InvalidOperationException($"Keyword '{name}' уже зарегистрирован.");

        _registrations[name] = new KeywordRegistration(name, handlerType, argsType, category, description);
    }

    public KeywordRegistration Get(string name)
    {
        if (!_registrations.TryGetValue(name, out var registration))
            throw new KeyNotFoundException($"Keyword '{name}' не зарегистрирован.");

        return registration;
    }

    public IReadOnlyCollection<KeywordRegistration> GetAll() =>
        _registrations.Values
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    IReadOnlyCollection<KeywordMetadata> IKeywordMetadataProvider.GetKeywords() =>
        _registrations.Values
            .Select(r => new KeywordMetadata(r.Name, r.HandlerType.Name, r.Category, r.Description))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
