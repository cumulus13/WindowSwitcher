# Window Switcher

Fast, lightweight window switching utility for Windows with keyboard-driven navigation.

## 🎦 Demo

<div align="center">
  <a href="https://youtu.be/H2wqDc4WVy8">
    <img src="https://raw.githubusercontent.com/cumulus13/windowswitcher/master/screenshot.png" alt="How to use mks - tree2 -pt" style="width:100%;">
  </a>
  <br>
  <a href="https://youtu.be/H2wqDc4WVy8">Demo</a>
</div>

## Features

- 🚀 **Fast window search** - Real-time filtering as you type
- ⌨️ **Keyboard shortcuts** - Configurable global hotkey (default: Alt+Space)
- 🎨 **Window icons** - Optional icon display (configurable)
- ⚙️ **Customizable** - TOML configuration file
- 💡 **Clean UI** - Modern, minimal floating dialog
- 🎯 **Smart filtering** - Search by window title or process name

## Installation & Build

### Prerequisites

1. Install .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0

### Build Instructions

```bash
# Clone or extract the project
cd WindowSwitcher

# Restore dependencies
dotnet restore

# Run in development mode
dotnet run

# Build release executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Executable will be at:
# bin/Release/net8.0-windows/win-x64/publish/WindowSwitcher.exe
```

## Usage

### Running the Application

1. Start `WindowSwitcher.exe`
2. Press **Alt+Space** (or your configured hotkey) to open the switcher
3. Type to filter windows
4. Use **Arrow keys** or **mouse** to select
5. Press **Enter** or **double-click** to switch to window
6. Press **Escape** to close without switching

### Keyboard Shortcuts

- **Alt+Space** - Toggle window switcher (configurable)
- **Up/Down Arrow** - Navigate window list
- **Enter** - Switch to selected window
- **Escape** - Close switcher

## Configuration

Configuration file is automatically created at:
```
%APPDATA%\WindowSwitcher\config.toml
```

### Default Configuration

```toml
# Window Switcher Configuration

[general]
# Show window icons (true/false)
show_icons = true

# Maximum number of windows to show in list
max_list_show = 50

# Enable dark theme (true/false)
dark_theme = false

[hotkey]
# Hotkey modifier: Alt, Control, Shift, Windows
modifier = "Alt"

# Hotkey key: Space, Tab, etc.
# Common keys: Space, Tab, Enter, F1-F12, A-Z, D0-D9
key = "Space"
```

### Available Options

#### General Settings
- `show_icons` - Display window icons (true/false)
- `max_list_show` - Maximum windows in list (1-1000)
- `dark_theme` - Enable dark theme (true/false)

#### Hotkey Settings
- `modifier` - Options: Alt, Control, Shift, Windows
- `key` - Any valid key: Space, Tab, F1-F12, A-Z, etc.

### Example Configurations

**Use Ctrl+Q:**
```toml
[hotkey]
modifier = "Control"
key = "Q"
```

**Hide icons for better performance:**
```toml
[general]
show_icons = false
```

**Show only top 20 windows:**
```toml
[general]
max_list_show = 20
```

**Enable dark theme:**
```toml
[general]
dark_theme = true
```

## Troubleshooting

### Hotkey not working
- Check if another application is using the same hotkey
- Try a different key combination in config.toml
- Restart the application after changing config

### Icons not showing
- Set `show_icons = true` in config.toml
- Some system windows may not have icons
- Icon extraction requires appropriate permissions

### Application not starting
- Ensure .NET 8 Runtime is installed (if not self-contained)
- Check Windows Event Viewer for errors
- Run from command line to see error messages

## System Requirements

- Windows 10/11
- .NET 8 Runtime (if not using self-contained build)
- ~10MB RAM
- Minimal CPU usage

## License

MIT License - Feel free to modify and distribute.

## Credits

Built with:
- .NET 8 / WPF
- Tomlyn (TOML parser)
- Win32 API

---

## 💻 Author

[**Hadi Cahyadi**](mailto:cumulus13@gmail.com)

- GitHub: [@cumulus13](https://github.com/cumulus13)
- Email: cumulus13@gmail.com

## 💖 Support

- 🐛 **Bug Reports**: [GitHub Issues](https://github.com/cumulus13/windowswitcher/issues)
- 💡 **Feature Requests**: [GitHub Discussions](https://github.com/cumulus13/windowswitcher/discussions)
- 📧 **Email**: cumulus13@gmail.com

**Made with ❤️ by Hadi Cahyadi**

[![Buy Me a Coffee](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/cumulus13)

[![Donate via Ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/cumulus13)
 
[Support me on Patreon](https://www.patreon.com/cumulus13)

