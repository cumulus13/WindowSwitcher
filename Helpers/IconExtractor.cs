// File: Helpers\IconExtractor.cs
// Author: Hadi Cahyadi <cumulus13@gmail.com>
// Date: 2025-12-27
// Description: 
// License: MIT

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WindowSwitcher.Native;
using System.Windows.Interop;

namespace WindowSwitcher.Helpers;

public static class IconExtractor
{
    // ✅ CACHE ICON FOR PROCESS ID — SAFE & FAST
    private static readonly ConcurrentDictionary<uint, ImageSource> _iconCache = new();

    public static ImageSource? ExtractIcon(IntPtr hWnd, uint processId)
    {
        // 1. Try taking it from cache first
        if (_iconCache.TryGetValue(processId, out var cachedIcon))
        {
            return cachedIcon;
        }

        IntPtr iconHandle = IntPtr.Zero;

        try
        {
            // 2. Try taking it from the message window
            iconHandle = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, 
                (IntPtr)NativeMethods.ICON_SMALL, IntPtr.Zero);

            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, 
                    (IntPtr)NativeMethods.ICON_BIG, IntPtr.Zero);
            }

            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, 
                    (IntPtr)NativeMethods.ICON_SMALL2, IntPtr.Zero);
            }

            // 3. If it's still null, try from the executable
            if (iconHandle == IntPtr.Zero)
            {
                try
                {
                    using var process = Process.GetProcessById((int)processId);
                    string? exePath = process.MainModule?.FileName;

                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    {
                        iconHandle = NativeMethods.ExtractIcon(IntPtr.Zero, exePath, 0);
                    }
                }
                catch
                {
                    // The process is closed / cannot be accessed
                }
            }

            // 4. Convert to WPF ImageSource
            if (iconHandle != IntPtr.Zero)
            {
                try
                {
                    // 💥 CAUTION: DO NOT Dispose the icon if the handle is from SendMessage!
                    // SendMessage returns the handle owned by the system.
                    // So we CANNOT use `using` here.

                    var source = Imaging.CreateBitmapSourceFromHIcon(
                        iconHandle,
                        // Int32Rect.Empty,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(32, 32)
                    );

                    // ✅ MANDATORY: Freeze so it can be used in multi-thread & in cache
                    source.Freeze();

                    // save cache
                    _iconCache.TryAdd(processId, source);

                    return source;
                }
                catch
                {
                    // Conversion failed
                }
            }
        }
        catch
        {
            // Common errors — ignore them
        }

        // Fallback: return null (or default icon if you want)
        return null;
    }

    // Optional: to debug or reset
    public static void ClearCache() => _iconCache.Clear();
}

// using System;
// using System.Diagnostics;
// using System.Windows;
// using System.Windows.Interop;
// using System.Windows.Media;
// using System.Windows.Media.Imaging;
// using WindowSwitcher.Native;
// using DrawingIcon = System.Drawing.Icon;

// namespace WindowSwitcher.Helpers;

// public static class IconExtractor
// {
//     public static ImageSource? ExtractIcon(IntPtr hWnd, uint processId)
//     {
//         try
//         {
//             // Try to get icon from window
//             IntPtr iconHandle = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, 
//                 (IntPtr)NativeMethods.ICON_SMALL, 0);

//             if (iconHandle == IntPtr.Zero)
//             {
//                 iconHandle = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, 
//                     (IntPtr)NativeMethods.ICON_BIG, 0);
//             }

//             if (iconHandle == IntPtr.Zero)
//             {
//                 iconHandle = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, 
//                     (IntPtr)NativeMethods.ICON_SMALL2, 0);
//             }

//             // If still no icon, try to get from process executable
//             if (iconHandle == IntPtr.Zero)
//             {
//                 try
//                 {
//                     var process = Process.GetProcessById((int)processId);
//                     string exePath = process.MainModule?.FileName ?? string.Empty;
                    
//                     if (!string.IsNullOrEmpty(exePath))
//                     {
//                         iconHandle = NativeMethods.ExtractIcon(IntPtr.Zero, exePath, 0);
//                     }
//                 }
//                 catch
//                 {
//                     // Process might have exited or no access
//                 }
//             }

//             // Convert to ImageSource
//             if (iconHandle != IntPtr.Zero)
//             {
//                 try
//                 {
//                     using var icon = DrawingIcon.FromHandle(iconHandle);
//                     return Imaging.CreateBitmapSourceFromHIcon(
//                         icon.Handle,
//                         Int32Rect.Empty,
//                         BitmapSizeOptions.FromEmptyOptions());
//                 }
//                 catch
//                 {
//                     // Failed to convert icon
//                 }
//             }
//         }
//         catch
//         {
//             // Failed to extract icon
//         }

//         return null;
//     }
// }
