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
dark_theme = false

[hotkey]
# Hotkey modifier: Alt, Control, Shift, Windows
modifier = ""Alt""

# Hotkey key: Space, Tab, etc.
# Common keys: Space, Tab, Enter, F1-F12, A-Z, D0-D9
key = ""Space""
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
                // Try to parse show_icons
                try
                {
                    if (general.TryGetValue("show_icons", out var showIcons))
                    {
                        settings.ShowIcons = showIcons switch
                        {
                            bool b => b,
                            string s => bool.Parse(s),
                            long l => l != 0,
                            _ => true
                        };
                    }
                }
                catch
                {
                    settings.ShowIcons = true; // Use default on error
                }

                // Try to parse max_list_show
                try
                {
                    if (general.TryGetValue("max_list_show", out var maxList))
                    {
                        settings.MaxListShow = maxList switch
                        {
                            int i => i,
                            long l => (int)l,
                            string s => int.Parse(s),
                            _ => 50
                        };
                    }
                }
                catch
                {
                    settings.MaxListShow = 50; // Use default on error
                }

                // Try to parse dark_theme
                try
                {
                    if (general.TryGetValue("dark_theme", out var darkTheme))
                    {
                        settings.DarkTheme = darkTheme switch
                        {
                            bool b => b,
                            string s => bool.Parse(s),
                            long l => l != 0,
                            _ => false
                        };
                    }
                }
                catch
                {
                    settings.DarkTheme = false; // Use default on error
                }
            }

            // Load hotkey settings
            if (model.TryGetValue("hotkey", out var hotkeyObj) && hotkeyObj is TomlTable hotkey)
            {
                // Try to parse modifier
                try
                {
                    if (hotkey.TryGetValue("modifier", out var modifier))
                    {
                        string modStr = modifier?.ToString() ?? "Alt";
                        settings.HotkeyModifierString = modStr;
                        settings.HotkeyModifier = ParseModifier(modStr);
                    }
                }
                catch
                {
                    settings.HotkeyModifier = ModifierKeys.Alt; // Use default on error
                    settings.HotkeyModifierString = "Alt";
                }

                // Try to parse key
                try
                {
                    if (hotkey.TryGetValue("key", out var key))
                    {
                        string keyStr = key?.ToString() ?? "Space";
                        settings.HotkeyKeyString = keyStr;
                        settings.HotkeyKey = ParseKey(keyStr);
                    }
                }
                catch
                {
                    settings.HotkeyKey = Key.Space; // Use default on error
                    settings.HotkeyKeyString = "Space";
                }
            }

            return settings;
        }
        catch (Exception ex)
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
            // Log the error or handle it as needed
            Console.WriteLine($"Failed to create default config: {ex.Message}");
        }
    }

    private ModifierKeys ParseModifier(string modifier)
    {
        return modifier.ToLowerInvariant() switch
        {
            "alt" => ModifierKeys.Alt,
            "control" or "ctrl" => ModifierKeys.Control,
            "shift" => ModifierKeys.Shift,
            "windows" or "win" => ModifierKeys.Windows,
            _ => ModifierKeys.Alt
        };
    }

    private Key ParseKey(string key)
    {
        if (Enum.TryParse<Key>(key, true, out var result))
            return result;
        return Key.Space;
    }
}