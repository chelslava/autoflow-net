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
        if (_disposed)
            return;

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed || _manager is null)
                return;

            await _manager.DisposeAsync().ConfigureAwait(false);
            _manager = null;
            _disposed = true;
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
}
