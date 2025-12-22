using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WindowSwitcher.Native;
using DrawingIcon = System.Drawing.Icon;

namespace WindowSwitcher.Helpers;

public static class IconExtractor
{
    public static ImageSource? ExtractIcon(IntPtr hWnd, uint processId)
    {
        try
        {
            // Try to get icon from window
            IntPtr iconHandle = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, 
                (IntPtr)NativeMethods.ICON_SMALL, 0);

            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, 
                    (IntPtr)NativeMethods.ICON_BIG, 0);
            }

            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = NativeMethods.SendMessage(hWnd, NativeMethods.WM_GETICON, 
                    (IntPtr)NativeMethods.ICON_SMALL2, 0);
            }

            // If still no icon, try to get from process executable
            if (iconHandle == IntPtr.Zero)
            {
                try
                {
                    var process = Process.GetProcessById((int)processId);
                    string exePath = process.MainModule?.FileName ?? string.Empty;
                    
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        iconHandle = NativeMethods.ExtractIcon(IntPtr.Zero, exePath, 0);
                    }
                }
                catch
                {
                    // Process might have exited or no access
                }
            }

            // Convert to ImageSource
            if (iconHandle != IntPtr.Zero)
            {
                try
                {
                    using var icon = DrawingIcon.FromHandle(iconHandle);
                    return Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
                catch
                {
                    // Failed to convert icon
                }
            }
        }
        catch
        {
            // Failed to extract icon
        }

        return null;
    }
}