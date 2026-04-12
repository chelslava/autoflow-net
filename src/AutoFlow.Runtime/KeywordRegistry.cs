using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    public KeywordRegistry RegisterKeywordsFromAssembly(Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Select(type => new
            {
                Type = type,
                Attribute = type.GetCustomAttribute<KeywordAttribute>(),
                HandlerInterface = type.GetInterfaces()
                    .FirstOrDefault(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IKeywordHandler<>))
            })
            .Where(x => x.Attribute is not null && x.HandlerInterface is not null)
            .ToList();

        foreach (var item in handlerTypes)
        {
            var argsType = item.HandlerInterface!.GetGenericArguments()[0];

            Register(
                item.Attribute!.Name,
                item.Type,
                argsType,
                item.Attribute.Category,
                item.Attribute.Description);
        }

        return this;
    }
}
