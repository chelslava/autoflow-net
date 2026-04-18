using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AutoFlow.Runtime.Resilience;

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public sealed class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 5;
    public int ResetTimeoutSeconds { get; set; } = 30;
    public int HalfOpenMaxAttempts { get; set; } = 1;
}

public sealed class CircuitBreaker
{
    private readonly ConcurrentDictionary<string, CircuitStateInfo> _circuits = new();
    private CircuitBreakerOptions _options;

    public CircuitBreaker(CircuitBreakerOptions? options = null)
    {
        _options = options ?? new CircuitBreakerOptions();
    }

    public void UpdateOptions(CircuitBreakerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public CircuitState GetState(string key)
    {
        var info = _circuits.GetOrAdd(key, _ => new CircuitStateInfo());
        lock (info)
        {
            if (info.State == CircuitState.Open)
            {
                var elapsed = DateTime.UtcNow - info.LastFailureUtc;
                if (elapsed.TotalSeconds >= _options.ResetTimeoutSeconds)
                {
                    info.State = CircuitState.HalfOpen;
                    info.HalfOpenAttempts = 0;
                }
            }
            return info.State;
        }
    }

    public bool CanExecute(string key)
    {
        var state = GetState(key);
        if (state == CircuitState.Closed)
            return true;

        if (state == CircuitState.HalfOpen)
        {
            var info = _circuits.GetOrAdd(key, _ => new CircuitStateInfo());
            lock (info)
            {
                if (info.HalfOpenAttempts < _options.HalfOpenMaxAttempts)
                {
                    info.HalfOpenAttempts++;
                    return true;
                }
                return false;
            }
        }

        return false;
    }

    public void RecordSuccess(string key)
    {
        var info = _circuits.GetOrAdd(key, _ => new CircuitStateInfo());
        lock (info)
        {
            info.FailureCount = 0;
            info.State = CircuitState.Closed;
            info.HalfOpenAttempts = 0;
        }
    }

    public void RecordFailure(string key)
    {
        var info = _circuits.GetOrAdd(key, _ => new CircuitStateInfo());
        lock (info)
        {
            info.FailureCount++;
            info.LastFailureUtc = DateTime.UtcNow;

            if (info.State == CircuitState.HalfOpen)
            {
                info.State = CircuitState.Open;
                info.HalfOpenAttempts = 0;
            }
            else if (info.FailureCount >= _options.FailureThreshold)
            {
                info.State = CircuitState.Open;
            }
        }
    }

    public void Reset(string key)
    {
        _circuits.TryRemove(key, out _);
    }

    public void ResetAll()
    {
        _circuits.Clear();
    }

    private sealed class CircuitStateInfo
    {
        public CircuitState State = CircuitState.Closed;
        public int FailureCount;
        public DateTime LastFailureUtc = DateTime.MinValue;
        public int HalfOpenAttempts;
    }
}
