// =============================================================================
// BrowserManagerProvider.cs — глобальный доступ к BrowserManager.
// =============================================================================

using System;

namespace AutoFlow.Library.Browser;

/// <summary>
/// Предоставляет глобальный доступ к BrowserManager.
/// Используется для backward compatibility со статическими методами.
/// </summary>
public static class BrowserManagerProvider
{
    private static BrowserManager? _manager;

    public static BrowserManager? Manager => _manager;

    public static void Initialize(BrowserManager manager)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    public static async void Dispose()
    {
        if (_manager is not null)
        {
            await _manager.DisposeAsync().ConfigureAwait(false);
            _manager = null;
        }
    }
}
