using System.Windows.Input;

namespace WindowSwitcher.Models;

public class AppSettings
{
    public bool ShowIcons { get; set; } = true;
    public int MaxListShow { get; set; } = 50;
    public ModifierKeys HotkeyModifier { get; set; } = ModifierKeys.Alt;
    public Key HotkeyKey { get; set; } = Key.Space;
    public string HotkeyModifierString { get; set; } = "Alt";
    public string HotkeyKeyString { get; set; } = "Space";
    public bool DarkTheme { get; set; } = false;
    
    // Multi-monitor settings
    public bool FollowCursor { get; set; } = true;
    public ModifierKeys ToggleModeModifier { get; set; } = ModifierKeys.Alt | ModifierKeys.Shift;
    public Key ToggleModeKey { get; set; } = Key.M;
    public string ToggleModeModifierString { get; set; } = "Alt+Shift";
    public string ToggleModeKeyString { get; set; } = "M";

    // Cache settings
    public int CacheSeconds { get; set; } = 5;
    public int AutoRefreshSeconds { get; set; } = 30;
}