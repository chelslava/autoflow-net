using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoFlow.Abstractions;

public sealed class SecretMasker
{
    private const int DefaultMaxSecrets = 1000;
    private const int MinSecretLength = 4;

    private readonly LinkedList<string> _orderedSecrets = new();
    private readonly HashSet<string> _secrets = new(StringComparer.Ordinal);
    private readonly object _lock = new();
    private readonly int _maxSecrets;

    public SecretMasker(int maxSecrets = DefaultMaxSecrets)
    {
        if (maxSecrets <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSecrets), "Max secrets must be positive.");

        _maxSecrets = maxSecrets;
    }

    public void RegisterSecret(string secret)
    {
        if (string.IsNullOrEmpty(secret) || secret.Length < MinSecretLength)
            return;

        lock (_lock)
        {
            if (_secrets.Contains(secret))
                return;

            if (_secrets.Count >= _maxSecrets)
            {
                var oldest = _orderedSecrets.First?.Value;
                if (oldest is not null)
                {
                    _orderedSecrets.RemoveFirst();
                    _secrets.Remove(oldest);
                }
            }

            _secrets.Add(secret);
            _orderedSecrets.AddLast(secret);
        }
    }

    public void RegisterSecrets(IEnumerable<string> secrets)
    {
        foreach (var secret in secrets)
            RegisterSecret(secret);
    }

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

    public void Clear()
    {
        lock (_lock)
        {
            _secrets.Clear();
            _orderedSecrets.Clear();
        }
    }

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

    public int MaxSecrets => _maxSecrets;
}
