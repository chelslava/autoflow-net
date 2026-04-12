// Этот код нужен для регистрации и поиска обработчиков keyword.
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoFlow.Runtime;

public sealed class KeywordRegistry
{
    private readonly Dictionary<string, KeywordRegistration> _registrations =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(string name, Type handlerType, Type argsType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя keyword не может быть пустым.", nameof(name));

        if (_registrations.ContainsKey(name))
            throw new InvalidOperationException($"Keyword '{name}' уже зарегистрирован.");

        _registrations[name] = new KeywordRegistration(name, handlerType, argsType);
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
}
