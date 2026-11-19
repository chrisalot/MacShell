<div align="center">

# 🖥️ MacShell

**A Mac-inspired desktop shell for Windows — native, premium, performant.**

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/WinUI-3-0078D7?logo=windows&logoColor=white)](https://learn.microsoft.com/windows/apps/winui/)
[![Windows App SDK](https://img.shields.io/badge/Windows%20App%20SDK-1.6-0078D7?logo=windows&logoColor=white)](https://learn.microsoft.com/windows/apps/windows-app-sdk/)
[![Build](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/chrisalot/macshell/actions)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D7?logo=windows&logoColor=white)](https://www.microsoft.com/windows)

> **Not a toy. Not a web wrapper. Not Electron.** A native, premium, performant desktop environment for Windows.

</div>

---

## ✨ Features

| Feature | Status | Details |
|---------|--------|---------|
| **Bottom Dock** | ✅ Implemented | App icons with hover magnification, running indicators, right-click context menus (Pin/Unpin/Quit), drag-to-reorder |
| **Top Menu Bar** | ✅ Implemented | Time display, battery status, network status (WiFi/Ethernet) |
| **App Launcher** (Spotlight-style) | ✅ Implemented | Win+Space hotkey, fast app search, keyboard navigation |
| **Window Management** | ✅ Implemented | Win32 integration, activate/minimize/maximize/close |
| **Settings** | ✅ Implemented | Theme (Dark/Light/System), accent colors, dock behavior, widget management |
| **Widget System** | ✅ Implemented | Hot-reload support, isolated `AssemblyLoadContext`, settings persistence, 3 built-in widgets |

---

## 🧩 Built-in Widgets

| Widget | Description |
|--------|-------------|
| 🕐 **Clock** | Time and date with configurable format |
| 📊 **System Monitor** | Real-time CPU, memory, network usage graphs |
| 🌤️ **Weather** | Current conditions via Open-Meteo API |

---

## 📸 Screenshots

<div align="center">

| Dock & Top Bar | App Launcher | Settings |
|:---:|:---:|:---:|
| ![Dock](docs/screenshots/dock.png) | ![Launcher](docs/screenshots/launcher.png) | ![Settings](docs/screenshots/settings.png) |

*Run MacShell to see it in action — screenshots coming to the repo soon!*

</div>

---

## 🚀 Quick Start

### Requirements
- **Windows 10** 19041+ (Windows 11 recommended)
- **.NET 9 Runtime** (self-contained in Release build — no install needed!)
- **Windows App SDK 1.6+** (bundled)

### Download & Run
```bash
# Clone
git clone https://github.com/chrisalot/macshell.git
cd macshell

# Build Release (self-contained single file ~150MB)
.\build.ps1 -Configuration Release

# Run
.\artifacts\bin\Release\net9.0-windows10.0.19041.0\win-x64\MacShell.exe
```

### Keyboard Shortcuts
| Shortcut | Action |
|----------|--------|
| `Win` + `Space` | Open App Launcher |
| `Win` + `,` | Show Settings |
| `Right-click` dock item | Context menu (Pin/Unpin/Quit) |
| `Drag` dock item | Reorder |

---

## 🏗️ Building

```bash
# Release (single-file, self-contained, optimized)
.\build.ps1 -Configuration Release

# Debug (with symbols, faster iteration)
.\build.ps1 -Configuration Debug

# Run tests
.\build.ps1 -Target Test

# Clean build artifacts
.\build.ps1 -Target Clean
```

### Output Locations

| Configuration | Output |
|---------------|--------|
| **Release** | `artifacts/bin/Release/net9.0-windows10.0.19041.0/win-x64/MacShell.exe` (~150 MB) |
| **Debug** | `artifacts/bin/Debug/net9.0-windows10.0.19041.0/win-x64/MacShell.exe` |

---

## 🏛️ Architecture

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

---

## 🔌 Extensibility — Build Your Own Widgets

MacShell is a **platform**, not a closed app. Create custom widgets in C#:

1. **Create** a Class Library targeting `net9.0-windows10.0.19041.0`
2. **Reference** `MacShell.Core` and `MacShell.UI`
3. **Implement** `IWidget` interface
4. **Add** `widget.json` manifest
5. **Build** and place in `%LOCALAPPDATA%\MacShell\UserWidgets\YourWidget\`
6. **Enjoy** auto-detection with **hot-reload** support!

📖 See [Widget Development Guide](docs/widget-development.md) for full details.

---

## ⚡ Performance Targets

| Metric | Target |
|--------|--------|
| **Idle RAM** | < 80 MB |
| **Idle CPU** | ~0% (event-driven, no polling) |
| **Cold Start** | < 2 seconds |
| **Animation** | 60fps (Composition API) |
| **Binary Size** | ~150 MB (single-file, self-contained) |

---

## 🛠️ Tech Stack

| Category | Technology |
|----------|------------|
| **Language** | C# 12 |
| **UI Framework** | WinUI 3 (Windows App SDK 1.6) |
| **DI / Logging** | Microsoft.Extensions.* |
| **MVVM** | CommunityToolkit.Mvvm 8.3 |
| **Serialization** | System.Text.Json (source-generated) |
| **Build** | MSBuild / dotnet CLI |
| **Analysis** | StyleCop.Analyzers, .NET Analyzers |

---

## 🎨 Design Principles

- **🌿 Native First** — WinUI 3, not Electron/Tauri/WebView
- **⚡ Performance Obsessed** — No polling, Composition API animations, virtualized UI
- **🍎 Mac Aesthetic** — Acrylic, rounded corners, SF-inspired typography, 8pt spacing grid
- **🔌 Extensible** — Plugin architecture with isolated `AssemblyLoadContext`
- **🧹 Maintainable** — Clean architecture, strict DI, comprehensive documentation

---

## 📋 Roadmap

- [ ] Multiple monitor support
- [ ] App menu integration (left side of top bar)
- [ ] Desktop widgets (not just dock/top bar)
- [ ] Window snapping/tiling
- [ ] Virtual desktops integration
- [ ] Plugin marketplace
- [ ] Themes marketplace

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests and build (`.\build.ps1 -Configuration Release`)
5. Submit a PR

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

---

## 💬 Support & Community

- **Issues**: [GitHub Issues](https://github.com/chrisalot/macshell/issues)
- **Discussions**: [GitHub Discussions](https://github.com/chrisalot/macshell/discussions)
- **Wiki**: [Documentation](https://github.com/chrisalot/macshell/wiki)

---

<div align="center">

*Built with ❤️ for Windows users who miss the Mac experience.*

**Star ⭐ this repo if you like it!**

</div>