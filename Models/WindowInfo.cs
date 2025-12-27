// File: Models\WindowInfo.cs
// Author: Hadi Cahyadi <cumulus13@gmail.com>
// Date: 2025-12-27
// Description: 
// License: MIT

using System;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;

namespace WindowSwitcher.Models;

public class WindowInfo : INotifyPropertyChanged
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public uint ProcessId { get; set; }
    // public ImageSource? Icon { get; set; }
    private ImageSource? _icon;
    public ImageSource? Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    public Visibility IconVisibility => Icon != null ? Visibility.Visible : Visibility.Collapsed;
}