using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

public interface ICircuitBreakerStateStore
{
    Task<CircuitStateData?> GetStateAsync(string key, CancellationToken cancellationToken = default);
    Task SetStateAsync(string key, CircuitStateData state, CancellationToken cancellationToken = default);
    Task<IEnumerable<KeyValuePair<string, CircuitStateData>>> GetAllAsync(CancellationToken cancellationToken = default);
}

public sealed record CircuitStateData
{
    public CircuitState State { get; init; } = CircuitState.Closed;
    public int FailureCount { get; init; }
    public DateTime LastFailureUtc { get; init; } = DateTime.MinValue;
    public int HalfOpenAttempts { get; init; }
}

public sealed class InMemoryCircuitBreakerStore : ICircuitBreakerStateStore
{
    private readonly ConcurrentDictionary<string, CircuitStateData> _state = new();

    public Task<CircuitStateData?> GetStateAsync(string key, CancellationToken cancellationToken = default)
    {
        _state.TryGetValue(key, out var data);
        return Task.FromResult(data);
    }

    public Task SetStateAsync(string key, CircuitStateData state, CancellationToken cancellationToken = default)
    {
        _state.AddOrUpdate(key, state, (_, _) => state);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<KeyValuePair<string, CircuitStateData>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<KeyValuePair<string, CircuitStateData>>>(_state);
    }
}

public sealed class CircuitBreaker
{
    private readonly ConcurrentDictionary<string, CircuitStateInfo> _circuits = new();
    private readonly ICircuitBreakerStateStore? _stateStore;
    private CircuitBreakerOptions _options;

    public CircuitBreaker(CircuitBreakerOptions? options = null, ICircuitBreakerStateStore? stateStore = null)
    {
        _options = options ?? new CircuitBreakerOptions();
        _stateStore = stateStore;
    }

    public void UpdateOptions(CircuitBreakerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public CircuitState GetState(string key)
    {
        var info = GetOrCreateStateInfo(key);
        lock (info)
        {
            if (info.State == CircuitState.Open)
            {
                var elapsed = DateTime.UtcNow - info.LastFailureUtc;
                if (elapsed.TotalSeconds >= _options.ResetTimeoutSeconds)
                {
                    info.State = CircuitState.HalfOpen;
                    info.HalfOpenAttempts = 0;
                    _ = PersistStateAsync(key, info);
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
            var info = GetOrCreateStateInfo(key);
            lock (info)
            {
                if (info.HalfOpenAttempts < _options.HalfOpenMaxAttempts)
                {
                    info.HalfOpenAttempts++;
                    _ = PersistStateAsync(key, info);
                    return true;
                }
                return false;
            }
        }

        return false;
    }

    public void RecordSuccess(string key)
    {
        var info = GetOrCreateStateInfo(key);
        lock (info)
        {
            info.FailureCount = 0;
            info.State = CircuitState.Closed;
            info.HalfOpenAttempts = 0;
            _ = PersistStateAsync(key, info);
        }
    }

    public void RecordFailure(string key)
    {
        var info = GetOrCreateStateInfo(key);
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
            _ = PersistStateAsync(key, info);
        }
    }

    public void Reset(string key)
    {
        _circuits.TryRemove(key, out _);
        _ = PersistStateAsync(key, null);
    }

    public void ResetAll()
    {
        _circuits.Clear();
    }

    public async Task LoadPersistedStateAsync(CancellationToken cancellationToken = default)
    {
        if (_stateStore is null) return;

        var allStates = await _stateStore.GetAllAsync(cancellationToken).ConfigureAwait(false);
        foreach (var (key, data) in allStates)
        {
            var info = new CircuitStateInfo
            {
                State = data.State,
                FailureCount = data.FailureCount,
                LastFailureUtc = data.LastFailureUtc,
                HalfOpenAttempts = data.HalfOpenAttempts
            };
            _circuits.TryAdd(key, info);
        }
    }

    private CircuitStateInfo GetOrCreateStateInfo(string key)
    {
        return _circuits.GetOrAdd(key, _ => new CircuitStateInfo());
    }

    private async Task PersistStateAsync(string key, CircuitStateInfo? info)
    {
        if (_stateStore is null) return;

        if (info is null)
        {
            await _stateStore.SetStateAsync(key, new CircuitStateData()).ConfigureAwait(false);
        }
        else
        {
            var data = new CircuitStateData
            {
                State = info.State,
                FailureCount = info.FailureCount,
                LastFailureUtc = info.LastFailureUtc,
                HalfOpenAttempts = info.HalfOpenAttempts
            };
            await _stateStore.SetStateAsync(key, data).ConfigureAwait(false);
        }
    }

    private sealed class CircuitStateInfo
    {
        public CircuitState State = CircuitState.Closed;
        public int FailureCount;
        public DateTime LastFailureUtc = DateTime.MinValue;
        public int HalfOpenAttempts;
    }
}
