# XUnity.AutoInstaller

A GUI tool for automatically installing and configuring Unity game translation plugins.

English | [简体中文](README.md)

## Features

- 🎮 Automatic Unity Game Detection
  - Unity Engine Version
  - Architecture (32/64-bit)
  - Script Backend (Mono/IL2CPP)

- 🛠 Automated Installation Process
  - Auto-install compatible BepInEx framework
  - Auto-install XUnity.AutoTranslator plugin
  - Auto-configure necessary runtime environment

- ⚙️ Graphical Configuration Interface
  - Support multiple translation services (Google, Bing, Baidu, etc.)
  - Custom font settings
  - Multiple text framework configurations

- 🌈 Smart Theme Switching
  - Auto-adapt to Windows dark/light mode
  - Blue-white and dark themes available

## Built-in Component Versions

- BepInEx Mono: v5.4.23.2
- BepInEx IL2CPP: v6.0.0-be.733
- XUnity.AutoTranslator: v5.4.4

## System Requirements

- Windows 7/8/10/11
- [Python 3.8+](https://www.python.org/downloads/)
- Administrator privileges (for system component installation)

## Installation

### Using Released Version

1. Download the latest version from [Releases](../../releases)
2. Run the executable directly

### Running from Source

1. Clone the repository
```bash
git clone https://github.com/yourusername/XUnity.AutoInstaller.git
cd XUnity.AutoInstaller
```

2. Install dependencies
```bash
pip install -r requirements.txt
```

3. Run the program
```bash
python main.py
```

## Usage Guide

1. Launch the program
2. Click "Select Game Directory" to choose the game directory where you want to install the translation plugin
3. The program will automatically detect game information
4. Click "Install Translation Plugin" to start installation
5. After installation, click "Initialize Plugin" for initial configuration
6. Use "Edit Plugin Configuration" to customize translation services and other settings

## FAQ

1. **Q: Why can't some games be detected?**  
   A: Make sure you select the directory containing the main game executable, not shortcuts or launcher directories.

2. **Q: Game won't start after installation?**  
   A: Check if you're running the game with administrator privileges, as some games require it for plugin loading.

3. **Q: How to uninstall the plugin?**  
   A: Click the "Uninstall Plugin" button in the program interface, which will automatically clean up all related files.

## Development

### Building the Program

Use the following command to build the executable:
```bash
python build.py
```

### Project Structure

```
XUnity.AutoInstaller/
├── main.py              # Program entry
├── build.py            # Build script
├── requirements.txt    # Project dependencies
├── core/              # Core functionality modules
├── gui/               # GUI modules
└── File/              # Resource files
```

## Contributing

Issues and Pull Requests are welcome!

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [BepInEx](https://github.com/BepInEx/BepInEx) - Unity game modding framework
- [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator) - Game translation plugin
- [ttkbootstrap](https://github.com/israel-dryer/ttkbootstrap) - Modern tkinter themes 