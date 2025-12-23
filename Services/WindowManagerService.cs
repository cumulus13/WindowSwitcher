// ============================================================================
// FILE: Services/WindowManagerService.cs - LOG TO EXECUTABLE DIR
// ============================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using WindowSwitcher.Helpers;
using WindowSwitcher.Models;
using WindowSwitcher.Native;

namespace WindowSwitcher.Services;

public class WindowManagerService
{
    private bool _debugMode = false; // SET TRUE untuk debug
    private string _logFile = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", 
        "WindowSwitcher_Debug.txt"
    );

    private void Log(string message)
    {
        if (!_debugMode) return;
        
        try
        {
            File.AppendAllText(_logFile, $"{DateTime.Now:HH:mm:ss} - {message}\n");
        }
        catch { }
    }

    public List<WindowInfo> GetAllWindows(bool loadIcons)
    {
        var windows = new List<WindowInfo>();
        var currentProcess = Process.GetCurrentProcess().Id;

        if (_debugMode)
        {
            // Clear old log
            try { File.Delete(_logFile); } catch { }
            Log("=== STARTING WINDOW ENUMERATION ===");
        }

        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            try
            {
                // Get window title FIRST
                int length = NativeMethods.GetWindowTextLength(hWnd);
                string windowTitle = "";
                if (length > 0)
                {
                    var title = new StringBuilder(length + 1);
                    NativeMethods.GetWindowText(hWnd, title, title.Capacity);
                    windowTitle = title.ToString();
                }

                // Skip invisible windows
                if (!NativeMethods.IsWindowVisible(hWnd))
                {
                    if (_debugMode && !string.IsNullOrWhiteSpace(windowTitle))
                        Log($"SKIP (invisible): {windowTitle}");
                    return true;
                }

                // Skip empty titles
                if (string.IsNullOrWhiteSpace(windowTitle))
                {
                    if (_debugMode)
                        Log($"SKIP (no title): hWnd={hWnd}");
                    return true;
                }

                // Get process info
                NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
                
                // Skip our own process
                if (processId == currentProcess)
                {
                    if (_debugMode)
                        Log($"SKIP (self): {windowTitle}");
                    return true;
                }

                string processName = "Unknown";
                try
                {
                    var process = Process.GetProcessById((int)processId);
                    processName = process.ProcessName;
                }
                catch { }

                // Get window styles
                int exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
                IntPtr owner = NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER);
                
                bool hasToolWindowStyle = (exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0;
                bool hasAppWindowStyle = (exStyle & NativeMethods.WS_EX_APPWINDOW) != 0;
                bool hasOwner = owner != IntPtr.Zero;

                // Log details
                if (_debugMode)
                {
                    Log($"Window: {windowTitle} ({processName})");
                    Log($"  ToolWindow={hasToolWindowStyle}, AppWindow={hasAppWindowStyle}, HasOwner={hasOwner}");
                }

                // Filter logic
                if (hasToolWindowStyle && !hasAppWindowStyle)
                {
                    if (_debugMode)
                        Log($"  -> SKIP (tool window without app style)");
                    return true;
                }

                if (hasOwner && !hasAppWindowStyle)
                {
                    if (_debugMode)
                        Log($"  -> SKIP (has owner without app style)");
                    return true;
                }

                if (_debugMode)
                    Log($"  -> INCLUDED ✓");

                var windowInfo = new WindowInfo
                {
                    Handle = hWnd,
                    Title = windowTitle,
                    ProcessName = processName
                };

                if (loadIcons)
                {
                    windowInfo.Icon = IconExtractor.ExtractIcon(hWnd, processId);
                }

                windows.Add(windowInfo);
            }
            catch (Exception ex)
            {
                if (_debugMode)
                    Log($"ERROR: {ex.Message}");
            }

            return true;
        }, IntPtr.Zero);

        if (_debugMode)
        {
            Log($"=== TOTAL WINDOWS: {windows.Count} ===");
            Log($"Log file: {_logFile}");
        }

        return windows;
    }

    public void BringWindowToFront(IntPtr hWnd)
    {
        try
        {
            if (NativeMethods.IsIconic(hWnd))
            {
                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
            }
            NativeMethods.SetForegroundWindow(hWnd);
        }
        catch { }
    }
}