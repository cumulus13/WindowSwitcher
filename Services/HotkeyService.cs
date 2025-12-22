using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WindowSwitcher.Native;

namespace WindowSwitcher.Services;

public class HotkeyService : IDisposable
{
    private const int HOTKEY_ID = 9000;
    private readonly Window _window;
    private HwndSource? _source;
    private Action? _hotkeyAction;
    private bool _disposed;

    public HotkeyService(Window window)
    {
        _window = window;
    }

    public void RegisterHotkey(ModifierKeys modifiers, Key key, Action action)
    {
        _hotkeyAction = action;

        var helper = new WindowInteropHelper(_window);
        
        // Make sure window handle is created
        if (helper.Handle == IntPtr.Zero)
        {
            helper.EnsureHandle();
        }
        
        _source = HwndSource.FromHwnd(helper.Handle);
        _source?.AddHook(HwndHook);

        uint modifierFlags = GetModifierFlags(modifiers);
        uint vkCode = (uint)KeyInterop.VirtualKeyFromKey(key);

        if (!NativeMethods.RegisterHotKey(helper.Handle, HOTKEY_ID, modifierFlags, vkCode))
        {
            int errorCode = Marshal.GetLastWin32Error();
            string modifierName = modifiers.ToString();
            string keyName = key.ToString();
            
            throw new InvalidOperationException(
                $"Failed to register hotkey {modifierName}+{keyName}. " +
                $"Error code: {errorCode}. " +
                "The hotkey might already be in use by another application. " +
                "Please change the hotkey in config.toml and restart the application.");
        }
    }

    private uint GetModifierFlags(ModifierKeys modifiers)
    {
        uint flags = 0;
        if (modifiers.HasFlag(ModifierKeys.Alt))
            flags |= NativeMethods.MOD_ALT;
        if (modifiers.HasFlag(ModifierKeys.Control))
            flags |= NativeMethods.MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift))
            flags |= NativeMethods.MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Windows))
            flags |= NativeMethods.MOD_WIN;
        return flags;
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            _hotkeyAction?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        var helper = new WindowInteropHelper(_window);
        NativeMethods.UnregisterHotKey(helper.Handle, HOTKEY_ID);
        _source?.RemoveHook(HwndHook);
        _disposed = true;
    }
}