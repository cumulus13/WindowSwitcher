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
}