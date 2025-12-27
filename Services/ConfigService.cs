using System;
using System.IO;
using System.Windows.Input;
using Tomlyn;
using Tomlyn.Model;
using WindowSwitcher.Models;

namespace WindowSwitcher.Services;

public class ConfigService
{
    private readonly string _configPath;
    private const string DefaultConfig = @"# Window Switcher Configuration

[general]
# Show window icons (true/false)
show_icons = true

# Maximum number of windows to show in list
max_list_show = 50

# Enable dark theme (true/false)
dark_theme = true

# Follow cursor to monitor (true) or always use primary monitor (false)
follow_cursor = true

[cache]
# Cache window list for N seconds (avoid re-scanning)
cache_seconds = 5

# Auto-refresh cache every N seconds (0 = disabled)
auto_refresh_seconds = 30

[hotkey]
# Main hotkey to show/hide window switcher
modifier = ""Alt""
key = ""Space""

# Toggle display mode hotkey (follow cursor vs primary monitor)
toggle_mode_modifier = ""Alt+Shift""
toggle_mode_key = ""M""
";

    public ConfigService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appData, "WindowSwitcher");
        Directory.CreateDirectory(appFolder);
        _configPath = Path.Combine(appFolder, "config.toml");
    }

    public AppSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                CreateDefaultConfig();
            }

            string tomlContent = File.ReadAllText(_configPath);
            var model = Toml.ToModel(tomlContent);

            var settings = new AppSettings();

            // Load general settings
            if (model.TryGetValue("general", out var generalObj) && generalObj is TomlTable general)
            {
                settings.ShowIcons = ParseBool(general, "show_icons", true);
                settings.MaxListShow = ParseInt(general, "max_list_show", 50);
                settings.DarkTheme = ParseBool(general, "dark_theme", true);
                settings.FollowCursor = ParseBool(general, "follow_cursor", true);
            }

            // Load hotkey settings
            if (model.TryGetValue("hotkey", out var hotkeyObj) && hotkeyObj is TomlTable hotkey)
            {
                // Main hotkey
                string modStr = ParseString(hotkey, "modifier", "Alt");
                settings.HotkeyModifierString = modStr;
                settings.HotkeyModifier = ParseModifier(modStr);
                
                string keyStr = ParseString(hotkey, "key", "Space");
                settings.HotkeyKeyString = keyStr;
                settings.HotkeyKey = ParseKey(keyStr);
                
                // Toggle mode hotkey
                string toggleModStr = ParseString(hotkey, "toggle_mode_modifier", "Alt+Shift");
                settings.ToggleModeModifierString = toggleModStr;
                settings.ToggleModeModifier = ParseModifier(toggleModStr);
                
                string toggleKeyStr = ParseString(hotkey, "toggle_mode_key", "M");
                settings.ToggleModeKeyString = toggleKeyStr;
                settings.ToggleModeKey = ParseKey(toggleKeyStr);
            }

            if (model.TryGetValue("cache", out var cacheObj) && cacheObj is TomlTable cache)
            {
                settings.CacheSeconds = ParseInt(cache, "cache_seconds", 5);
                settings.AutoRefreshSeconds = ParseInt(cache, "auto_refresh_seconds", 30);
            }

            return settings;
        }
        catch (Exception)
        {
            // If config is corrupted, recreate it
            try
            {
                CreateDefaultConfig();
            }
            catch { }

            // Return fresh defaults
            return new AppSettings();
        }
    }

    private void CreateDefaultConfig()
    {
        try
        {
            File.WriteAllText(_configPath, DefaultConfig);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create default config: {ex.Message}");
        }
    }

    private bool ParseBool(TomlTable table, string key, bool defaultValue)
    {
        try
        {
            if (table.TryGetValue(key, out var value))
            {
                return value switch
                {
                    bool b => b,
                    string s => bool.Parse(s),
                    long l => l != 0,
                    _ => defaultValue
                };
            }
        }
        catch { }
        return defaultValue;
    }

    private int ParseInt(TomlTable table, string key, int defaultValue)
    {
        try
        {
            if (table.TryGetValue(key, out var value))
            {
                return value switch
                {
                    int i => i,
                    long l => (int)l,
                    string s => int.Parse(s),
                    _ => defaultValue
                };
            }
        }
        catch { }
        return defaultValue;
    }

    private string ParseString(TomlTable table, string key, string defaultValue)
    {
        try
        {
            if (table.TryGetValue(key, out var value))
            {
                return value?.ToString() ?? defaultValue;
            }
        }
        catch { }
        return defaultValue;
    }

    private ModifierKeys ParseModifier(string modifier)
    {
        var parts = modifier.Split('+');
        ModifierKeys result = ModifierKeys.None;
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim().ToLowerInvariant();
            result |= trimmed switch
            {
                "alt" => ModifierKeys.Alt,
                "control" or "ctrl" => ModifierKeys.Control,
                "shift" => ModifierKeys.Shift,
                "windows" or "win" => ModifierKeys.Windows,
                _ => ModifierKeys.None
            };
        }
        
        return result == ModifierKeys.None ? ModifierKeys.Alt : result;
    }

    private Key ParseKey(string key)
    {
        if (Enum.TryParse<Key>(key, true, out var result))
            return result;
        return Key.Space;
    }
}