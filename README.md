# MacShell

A Mac-inspired desktop shell for Windows, built with **C# + WinUI 3 (Windows App SDK)**.

> **Not a toy. Not a web wrapper. Not Electron.** A native, premium, performant desktop environment for Windows.

## Features

| Feature | Status |
|---------|--------|
| **Bottom Dock** | ✅ Implemented |
| - App icons with hover magnification | ✅ |
| - Running indicators | ✅ |
| - Right-click context menus (Pin/Unpin/Quit) | ✅ |
| - Drag to reorder | ✅ |
| **Top Menu Bar** | ✅ Implemented |
| - Time display | ✅ |
| - Battery status | ✅ |
| - Network status (WiFi/Ethernet) | ✅ |
| **App Launcher (Spotlight-style)** | ✅ Implemented |
| - Win+Space hotkey | ✅ |
| - Fast app search | ✅ |
| - Keyboard navigation | ✅ |
| **Window Management** | ✅ Implemented |
| - Win32 integration | ✅ |
| - Activate/minimize/maximize/close | ✅ |
| **Settings** | ✅ Implemented |
| - Theme (Dark/Light/System) | ✅ |
| - Accent colors | ✅ |
| - Dock behavior | ✅ |
| - Widget management | ✅ |
| **Widget System** | ✅ Implemented |
| - Hot-reload support | ✅ |
| - Isolated AssemblyLoadContext | ✅ |
| - Settings persistence | ✅ |
| - Built-in widgets (3) | ✅ |

## Built-in Widgets

1. **Clock Widget** - Time and date with configurable format
2. **System Monitor Widget** - CPU, memory, network usage
3. **Weather Widget** - Current weather via Open-Meteo API

## Screenshots

*Coming soon...*

## Requirements

- Windows 10 19041+ (Windows 11 recommended)
- .NET 9 Runtime (self-contained in Release build)
- Windows App SDK 1.6+

## Building

```bash
# Clone
git clone https://github.com/chrisalot/macshell.git
cd macshell

# Build Release
.\build.ps1 -Configuration Release

# Build Debug
.\build.ps1 -Configuration Debug

# Run tests
.\build.ps1 -Target Test
```

### Output

Release builds are self-contained single-file executables in:
```
artifacts/bin/Release/net9.0-windows10.0.19041.0/win-x64/MacShell.exe
```

## Architecture

```
MacShell/
├── src/
│   ├── MacShell.Core/           # Core interfaces, models, services
│   │   ├── Interfaces/          # IWidget, IDockItem, ISystemService, etc.
│   │   ├── Models/              # WidgetManifest, DockItem, AppInfo, etc.
│   │   ├── Services/            # WidgetManager, DockManager, SystemMonitor, etc.
│   │   └── Extensibility/       # PluginAssemblyContext, WidgetFileWatcher
│   ├── MacShell.UI/             # WinUI 3 views and controls
│   │   ├── Controls/            # DockPanel, DockItemControl, TopBar, LauncherWindow, etc.
│   │   ├── Views/               # ShellWindow, SettingsWindow
│   │   ├── Converters/          # Value converters
│   │   └── Resources/           # Themes, Styles, Icons
│   ├── MacShell.Widgets/        # Built-in widgets
│   │   ├── ClockWidget/
│   │   ├── SystemMonitorWidget/
│   │   └── WeatherWidget/
│   └── MacShell.App/            # Application entry point
├── UserWidgets/                 # User widget folder (runtime)
├── docs/                        # Documentation
└── MacShell.sln
```

## Extensibility

MacShell is designed as a **platform**, not a closed app. Users can create custom widgets in C#:

1. Create a Class Library targeting `net9.0-windows10.0.19041.0`
2. Reference `MacShell.Core` and `MacShell.UI`
3. Implement `IWidget` interface
4. Add `widget.json` manifest
5. Build and place in `%LOCALAPPDATA%\MacShell\UserWidgets\YourWidget\`
6. MacShell auto-detects with **hot-reload** support!

See [Widget Development Guide](docs/widget-development.md) for full details.

## Performance Targets

| Metric | Target |
|--------|--------|
| Idle RAM | < 80 MB |
| Idle CPU | ~0% (event-driven, no polling) |
| Startup | < 2 seconds |
| Animation | 60fps (Composition API) |

## Tech Stack

- **Language**: C# 12
- **UI Framework**: WinUI 3 (Windows App SDK 1.6)
- **DI/Logging**: Microsoft.Extensions.*
- **MVVM**: CommunityToolkit.Mvvm
- **Serialization**: System.Text.Json (source-generated)
- **Build**: MSBuild / dotnet CLI

## Design Principles

- **Native First** - WinUI 3, not Electron/Tauri/WebView
- **Performance Obsessed** - No polling, Composition API animations, virtualized UI
- **Mac Aesthetic** - Acrylic, rounded corners, SF-inspired typography, 8pt spacing
- **Extensible** - Plugin architecture with isolated load contexts
- **Maintainable** - Clean architecture, strict DI, comprehensive documentation

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests and build
5. Submit a PR

## Roadmap

- [ ] Multiple monitor support
- [ ] App menu integration (left side of top bar)
- [ ] Desktop widgets (not just dock/top bar)
- [ ] Window snapping/tiling
- [ ] Virtual desktops integration
- [ ] Plugin marketplace
- [ ] Themes marketplace

## Support

- **Issues**: [GitHub Issues](https://github.com/macshell/macshell/issues)
- **Discussions**: [GitHub Discussions](https://github.com/macshell/macshell/discussions)
- **Wiki**: [Documentation](https://github.com/macshell/macshell/wiki)

---

*Built with ❤️ for Windows users who miss the Mac experience.*
