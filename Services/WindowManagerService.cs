using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using WindowSwitcher.Helpers;
using WindowSwitcher.Models;
using WindowSwitcher.Native;

namespace WindowSwitcher.Services;

public class WindowManagerService
{
    public List<WindowInfo> GetAllWindows(bool loadIcons)
    {
        var windows = new List<WindowInfo>();
        var currentProcess = Process.GetCurrentProcess().Id;

        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            try
            {
                // Skip invisible windows
                if (!NativeMethods.IsWindowVisible(hWnd))
                    return true;

                // Get window title
                int length = NativeMethods.GetWindowTextLength(hWnd);
                if (length == 0)
                    return true;

                var title = new StringBuilder(length + 1);
                NativeMethods.GetWindowText(hWnd, title, title.Capacity);
                string windowTitle = title.ToString();

                if (string.IsNullOrWhiteSpace(windowTitle))
                    return true;

                // Get process info
                NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
                
                // Skip our own process
                if (processId == currentProcess)
                    return true;

                string processName = "Unknown";
                try
                {
                    var process = Process.GetProcessById((int)processId);
                    processName = process.ProcessName;
                }
                catch
                {
                    // Process might have exited
                }

                // Skip tool windows and windows without taskbar presence
                int exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
                if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0)
                {
                    // Check if it has WS_EX_APPWINDOW which overrides tool window
                    if ((exStyle & NativeMethods.WS_EX_APPWINDOW) == 0)
                        return true;
                }

                // Skip owned windows (like dialogs) unless they have WS_EX_APPWINDOW
                IntPtr owner = NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER);
                if (owner != IntPtr.Zero && (exStyle & NativeMethods.WS_EX_APPWINDOW) == 0)
                    return true;

                var windowInfo = new WindowInfo
                {
                    Handle = hWnd,
                    Title = windowTitle,
                    ProcessName = processName
                };

                // Load icon if requested
                if (loadIcons)
                {
                    windowInfo.Icon = IconExtractor.ExtractIcon(hWnd, processId);
                }

                windows.Add(windowInfo);
            }
            catch
            {
                // Skip problematic windows
            }

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public void BringWindowToFront(IntPtr hWnd)
    {
        try
        {
            // Restore if minimized
            if (NativeMethods.IsIconic(hWnd))
            {
                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
            }

            // Bring to front
            NativeMethods.SetForegroundWindow(hWnd);
        }
        catch
        {
            // Window might have been closed
        }
    }
}