using System;
using System.Windows;
using WindowSwitcher.Native;

namespace WindowSwitcher.Services;

public class MonitorService
{
    /// <summary>
    /// Get the monitor where the cursor is currently located
    /// </summary>
    public static Rect GetCursorMonitorWorkArea()
    {
        // Get cursor position
        if (!NativeMethods.GetCursorPos(out NativeMethods.POINT cursorPos))
        {
            return SystemParameters.WorkArea;
        }
        
        // Get monitor from cursor position
        IntPtr hMonitor = NativeMethods.MonitorFromPoint(
            cursorPos, 
            NativeMethods.MONITOR_DEFAULTTONEAREST
        );
        
        if (hMonitor == IntPtr.Zero)
        {
            return SystemParameters.WorkArea;
        }
        
        // Get monitor info
        NativeMethods.MONITORINFO monitorInfo = new NativeMethods.MONITORINFO();
        monitorInfo.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(monitorInfo);
        
        if (!NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo))
        {
            return SystemParameters.WorkArea;
        }
        
        // Convert to WPF Rect
        return new Rect(
            monitorInfo.rcWork.Left,
            monitorInfo.rcWork.Top,
            monitorInfo.rcWork.Right - monitorInfo.rcWork.Left,
            monitorInfo.rcWork.Bottom - monitorInfo.rcWork.Top
        );
    }
    
    /// <summary>
    /// Get the primary monitor work area
    /// </summary>
    public static Rect GetPrimaryMonitorWorkArea()
    {
        return SystemParameters.WorkArea;
    }
}
