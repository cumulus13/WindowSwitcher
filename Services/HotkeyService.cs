using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WindowSwitcher.Native;

namespace WindowSwitcher.Services;

public class HotkeyService : IDisposable
{
    private readonly Window _window;
    private HwndSource? _source;
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private bool _disposed;

    public HotkeyService(Window window)
    {
        _window = window;
    }

    public void RegisterHotkey(ModifierKeys modifiers, Key key, Action action, int hotkeyId)
    {
        _hotkeyActions[hotkeyId] = action;

        var helper = new WindowInteropHelper(_window);
        
        // Make sure window handle is created
        if (helper.Handle == IntPtr.Zero)
        {
            helper.EnsureHandle();
        }
        
        if (_source == null)
        {
            _source = HwndSource.FromHwnd(helper.Handle);
            _source?.AddHook(HwndHook);
        }

        uint modifierFlags = GetModifierFlags(modifiers);
        uint vkCode = (uint)KeyInterop.VirtualKeyFromKey(key);

        if (!NativeMethods.RegisterHotKey(helper.Handle, hotkeyId, modifierFlags, vkCode))
        {
            int errorCode = Marshal.GetLastWin32Error();
            string modifierName = modifiers.ToString();
            string keyName = key.ToString();
            
            throw new InvalidOperationException(
                $"Failed to register hotkey {modifierName}+{keyName} (ID: {hotkeyId}). " +
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
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(hotkeyId, out var action))
            {
                action?.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        var helper = new WindowInteropHelper(_window);
        foreach (var hotkeyId in _hotkeyActions.Keys)
        {
            NativeMethods.UnregisterHotKey(helper.Handle, hotkeyId);
        }
        
        _source?.RemoveHook(HwndHook);
        _hotkeyActions.Clear();
        _disposed = true;
    }
}
