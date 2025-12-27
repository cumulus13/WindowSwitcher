// File: Services\WindowManagerService.cs
// Author: Hadi Cahyadi <cumulus13@gmail.com>
// Date: 2025-12-27
// Description: 
// License: MIT


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
    private bool _debugMode = false;
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
            try { File.Delete(_logFile); } catch { }
            Log("=== STARTING WINDOW ENUMERATION ===");
        }

        NativeMethods.EnumWindows((hWnd, lParam) =>
        {
            try
            {
                // STEP 1: Check if window is visible
                if (!NativeMethods.IsWindowVisible(hWnd))
                {
                    return true; // Skip invisible windows
                }

                // STEP 2: Get window title
                var windowText = new StringBuilder(256);
                int length = NativeMethods.GetWindowText(hWnd, windowText, windowText.Capacity);
                string windowTitle = windowText.ToString();

                // Skip if no title
                if (string.IsNullOrWhiteSpace(windowTitle))
                {
                    if (_debugMode)
                        Log($"SKIP (no title): hWnd={hWnd}");
                    return true;
                }

                // STEP 3: Get process info
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
                catch
                {
                    // Process might have exited
                }

                // STEP 4: Get window styles
                int exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
                IntPtr owner = NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER);
                
                bool hasToolWindowStyle = (exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0;
                bool hasAppWindowStyle = (exStyle & NativeMethods.WS_EX_APPWINDOW) != 0;
                bool hasOwner = owner != IntPtr.Zero;

                // if (_debugMode)
                // {
                //     Log($"Window: {windowTitle} ({processName})");
                //     Log($"  ToolWindow={hasToolWindowStyle}, AppWindow={hasAppWindowStyle}, HasOwner={hasOwner}");
                // }

                // STEP 5: Apply filtering (same logic as showme.cs - simpler!)
                // Skip tool windows unless they have WS_EX_APPWINDOW
                if (hasToolWindowStyle && !hasAppWindowStyle)
                {
                    if (_debugMode)
                        Log($"  -> SKIP (tool window)");
                    return true;
                }

                // Skip owned windows (dialogs) unless they have WS_EX_APPWINDOW
                if (hasOwner && !hasAppWindowStyle)
                {
                    if (_debugMode)
                        Log($"  -> SKIP (owned window)");
                    return true;
                }

                if (_debugMode)
                    Log($"  -> INCLUDED ✓");

                // STEP 6: Create WindowInfo
                var windowInfo = new WindowInfo
                {
                    Handle = hWnd,
                    Title = windowTitle,
                    ProcessName = processName,
                    ProcessId = processId

                };

                // STEP 7: Load icon with error handling
                if (loadIcons)
                {
                    try
                    {
                        windowInfo.Icon = IconExtractor.ExtractIcon(hWnd, processId);
                    }
                    catch (Exception ex)
                    {
                        if (_debugMode)
                            Log($"  Icon extraction failed: {ex.Message}");
                        // Continue without icon
                    }
                }

                if (_debugMode)
                {
                    Log($"Window: {windowTitle} ({processName})");
                    Log($"  ToolWindow={hasToolWindowStyle}, AppWindow={hasAppWindowStyle}, HasOwner={hasOwner}");
                }

                windows.Add(windowInfo);
            }
            catch (Exception ex)
            {
                if (_debugMode)
                    Log($"ERROR processing window: {ex.Message}");
            }

            return true; // Continue enumeration
        }, IntPtr.Zero);

        if (_debugMode)
        {
            Log($"=== TOTAL WINDOWS: {windows.Count} ===");
        }

        // Console.WriteLine($"[WindowManagerService] Retrieved {windows.Count} windows.");
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
        catch (Exception ex)
        {
            if (_debugMode)
                Log($"BringWindowToFront ERROR: {ex.Message}");
        }
    }
}

// public class WindowManagerService
// {
//     public List<WindowInfo> GetAllWindows(bool loadIcons)
//     {
//         var windows = new List<WindowInfo>();
//         var currentProcess = Process.GetCurrentProcess().Id;

//         NativeMethods.EnumWindows((hWnd, lParam) =>
//         {
//             // SIMPLE FILTER - Just like Program.cs!
//             if (NativeMethods.IsWindowVisible(hWnd))
//             {
//                 // Get title
//                 var windowText = new StringBuilder(256);
//                 NativeMethods.GetWindowText(hWnd, windowText, windowText.Capacity);
//                 string title = windowText.ToString();

//                 // Skip empty titles
//                 if (string.IsNullOrWhiteSpace(title))
//                     return true;

//                 // Get process
//                 NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
                
//                 // Skip self
//                 if (processId == currentProcess)
//                     return true;

//                 string processName = "Unknown";
//                 try
//                 {
//                     var process = Process.GetProcessById((int)processId);
//                     processName = process.ProcessName;
//                 }
//                 catch { }

//                 // Create window info
//                 var windowInfo = new WindowInfo
//                 {
//                     Handle = hWnd,
//                     Title = title,
//                     ProcessName = processName
//                 };

//                 // Load icon ONLY if requested (THIS IS SLOW!)
//                 if (loadIcons)
//                 {
//                     try
//                     {
//                         windowInfo.Icon = IconExtractor.ExtractIcon(hWnd, processId);
//                     }
//                     catch
//                     {
//                         // Ignore icon errors
//                     }
//                 }

//                 windows.Add(windowInfo);
//             }
//             return true;
//         }, IntPtr.Zero);

//         return windows;
//     }

//     public void BringWindowToFront(IntPtr hWnd)
//     {
//         try
//         {
//             if (NativeMethods.IsIconic(hWnd))
//             {
//                 NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
//             }
//             NativeMethods.SetForegroundWindow(hWnd);
//         }
//         catch { }
//     }
// }


// public class WindowManagerService
// {
//     public List<WindowInfo> GetAllWindows(bool loadIcons)
//     {
//         var windows = new List<WindowInfo>();
//         var currentProcessId = Process.GetCurrentProcess().Id;

//         NativeMethods.EnumWindows((hWnd, lParam) =>
//         {
//             // EXACT COPY from Program.cs - NO CHANGES!
//             if (NativeMethods.IsWindowVisible(hWnd))
//             {
//                 StringBuilder windowText = new StringBuilder(256);
//                 NativeMethods.GetWindowText(hWnd, windowText, windowText.Capacity);
//                 string title = windowText.ToString();

//                 // Get process info
//                 NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
                
//                 string processName = "Unknown";
//                 try
//                 {
//                     var process = Process.GetProcessById((int)processId);
//                     processName = process.ProcessName;
//                 }
//                 catch { }

//                 // Create window info
//                 var windowInfo = new WindowInfo
//                 {
//                     Handle = hWnd,
//                     Title = title,
//                     ProcessName = processName
//                 };

//                 // Load icon (optional, makes it slow)
//                 if (loadIcons)
//                 {
//                     try
//                     {
//                         windowInfo.Icon = IconExtractor.ExtractIcon(hWnd, processId);
//                     }
//                     catch { }
//                 }

//                 windows.Add(windowInfo);
//             }
//             return true; // Continue enumeration
//         }, IntPtr.Zero);

//         return windows;
//     }

//     public void BringWindowToFront(IntPtr hWnd)
//     {
//         try
//         {
//             if (NativeMethods.IsIconic(hWnd))
//             {
//                 NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);
//             }
//             NativeMethods.SetForegroundWindow(hWnd);
//         }
//         catch { }
//     }
// }