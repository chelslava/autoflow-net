// Этот код нужен для объявления metadata keyword-обработчиков.
using System;

namespace AutoFlow.Abstractions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class KeywordAttribute : Attribute
{
    public KeywordAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя keyword не может быть пустым.", nameof(name));

        Name = name;
    }

    public string Name { get; }

    public string? Category { get; init; }

    public string? Description { get; init; }
}
