// =============================================================================
// BrowserManagerProvider.cs — глобальный доступ к BrowserManager.
// =============================================================================

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoFlow.Library.Browser;

public static class BrowserManagerProvider
{
    private static BrowserManager? _manager;
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static bool _disposed;

    public static BrowserManager? Manager => _manager;

    public static void Initialize(BrowserManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        _lock.Wait();
        try
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BrowserManagerProvider));
            
            _manager = manager;
        }
        finally
        {
            _lock.Release();
        }
    }

    public static async Task DisposeAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed || _manager is null)
                return;

            var manager = _manager;
            _manager = null;
            _disposed = true;
            
            await manager.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public static void Reset()
    {
        _lock.Wait();
        try
        {
            _manager = null;
            _disposed = false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public static T? WithManager<T>(Func<BrowserManager, T> action) where T : class?
    {
        _lock.Wait();
        try
        {
            if (_disposed || _manager is null)
                return null;
            return action(_manager);
        }
        finally
        {
            _lock.Release();
        }
    }

    public static async Task<T?> WithManagerAsync<T>(Func<BrowserManager, Task<T>> action) where T : class?
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed || _manager is null)
                return null;
            return await action(_manager).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }
}
