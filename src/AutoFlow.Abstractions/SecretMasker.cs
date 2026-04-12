// =============================================================================
// SecretMasker.cs — маскирование секретов в строках.
//
// Хранит список известных секретов и заменяет их на *** в логах и отчётах.
// Используется в VariableResolver и JsonReportGenerator.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoFlow.Abstractions;

/// <summary>
/// Маскирует секреты в строках. Хранит список известных секретов.
/// </summary>
public sealed class SecretMasker
{
    private readonly HashSet<string> _secrets = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    /// <summary>Регистрирует секрет для маскирования.</summary>
    public void RegisterSecret(string secret)
    {
        if (string.IsNullOrEmpty(secret) || secret.Length < 4)
            return;

        lock (_lock)
        {
            _secrets.Add(secret);
        }
    }

    /// <summary>Регистрирует несколько секретов.</summary>
    public void RegisterSecrets(IEnumerable<string> secrets)
    {
        foreach (var secret in secrets)
            RegisterSecret(secret);
    }

    /// <summary>Маскирует секреты в строке, заменяя их на ***.</summary>
    public string Mask(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        var result = input;
        List<string> secretsCopy;

        lock (_lock)
        {
            secretsCopy = _secrets.ToList();
        }

        foreach (var secret in secretsCopy)
        {
            result = result.Replace(secret, "***", StringComparison.Ordinal);
        }

        return result;
    }

    /// <summary>Очищает все зарегистрированные секреты.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _secrets.Clear();
        }
    }

    /// <summary>Возвращает количество зарегистрированных секретов.</summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _secrets.Count;
            }
        }
    }
}
