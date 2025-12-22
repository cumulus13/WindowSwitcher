using System;
using System.Windows;
using System.Windows.Media;

namespace WindowSwitcher.Models;

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public ImageSource? Icon { get; set; }
    public Visibility IconVisibility => Icon != null ? Visibility.Visible : Visibility.Collapsed;
}