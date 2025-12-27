// File: Services\WindowCacheService.cs
// Author: Hadi Cahyadi <cumulus13@gmail.com>
// Date: 2025-12-27
// Description: Not Used
// License: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowSwitcher.Models;
using WindowSwitcher.Native;

namespace WindowSwitcher.Services;

public class WindowCacheService : IDisposable
{
    private readonly WindowManagerService _windowManager;
    private List<WindowInfo> _cachedWindows = new();
    private DateTime _lastUpdate = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Timer? _refreshTimer;
    private readonly int _cacheSeconds;
    private readonly int _autoRefreshSeconds;
    private bool _disposed;

    public WindowCacheService(WindowManagerService windowManager, int cacheSeconds = 5, int autoRefreshSeconds = 30)
    {
        _windowManager = windowManager;
        _cacheSeconds = cacheSeconds;
        _autoRefreshSeconds = autoRefreshSeconds;
        
        // Auto-refresh timer (optional)
        if (_autoRefreshSeconds > 0)
        {
            _refreshTimer = new Timer(async _ => await InvalidateAsync(), null, 
                TimeSpan.FromSeconds(_autoRefreshSeconds), 
                TimeSpan.FromSeconds(_autoRefreshSeconds));
        }
    }

    public async Task<List<WindowInfo>> GetWindowsAsync(bool loadIcons)
    {
        await _lock.WaitAsync();
        try
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastUpdate).TotalSeconds;

            // Return cached if still valid AND windows still exist
            if (elapsed < _cacheSeconds && _cachedWindows.Count > 0)
            {
                // Quick validation: check if cached windows still exist
                var validWindows = _cachedWindows
                    .Where(w => NativeMethods.IsWindow(w.Handle) && NativeMethods.IsWindowVisible(w.Handle))
                    .ToList();

                // If 90%+ still valid, return cache
                if (validWindows.Count >= _cachedWindows.Count * 0.9)
                {
                    return new List<WindowInfo>(validWindows);
                }
            }

            // Refresh cache
            var windows = await Task.Run(() => _windowManager.GetAllWindows(loadIcons));
            _cachedWindows = windows;
            _lastUpdate = now;
            
            return new List<WindowInfo>(windows);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task InvalidateAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _lastUpdate = DateTime.MinValue;
            _cachedWindows.Clear();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _refreshTimer?.Dispose();
        _lock.Dispose();
        _disposed = true;
    }
}


// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using WindowSwitcher.Models;

// namespace WindowSwitcher.Services;

// public class WindowCacheService : IDisposable
// {
//     private readonly WindowManagerService _windowManager;
//     private readonly int _cacheSeconds;
//     private readonly int _autoRefreshSeconds;

//     private List<WindowInfo> _cachedWindowsWithoutIcons;
//     private DateTime _lastCacheTime = DateTime.MinValue;
//     private bool _disposed = false;

//     public WindowCacheService(WindowManagerService windowManager, int cacheSeconds, int autoRefreshSeconds)
//     {
//         _windowManager = windowManager;
//         _cacheSeconds = cacheSeconds;
//         _autoRefreshSeconds = autoRefreshSeconds;
//     }

//     public List<WindowInfo> GetWindows(bool loadIcons)
//     {
//         if (loadIcons)
//             return _windowManager.GetAllWindows(true); // No cache for icons

//         if ((DateTime.Now - _lastCacheTime).TotalSeconds > _cacheSeconds || _cachedWindowsWithoutIcons == null)
//         {
//             _cachedWindowsWithoutIcons = _windowManager.GetAllWindows(false);
//             _lastCacheTime = DateTime.Now;
//         }

//         return _cachedWindowsWithoutIcons;
//     }

//     public void Dispose()
//     {
//         if (!_disposed)
//         {
//             _cachedWindowsWithoutIcons = null;
//             _disposed = true;
//         }
//     }
// }