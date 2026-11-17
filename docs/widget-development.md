# MacShell Widget Development Guide

This guide explains how to create custom widgets for MacShell. Widgets are .NET class libraries that implement the `IWidget` interface and can be loaded dynamically at runtime with hot-reload support.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Widget Structure](#widget-structure)
3. [Widget Manifest](#widget-manifest)
4. [Implementing IWidget](#implementing-iwidget)
5. [Widget Context & Services](#widget-context--services)
6. [Settings](#settings)
7. [UI Development](#ui-development)
8. [Building & Testing](#building--testing)
9. [Hot Reload](#hot-reload)
10. [Distribution](#distribution)
11. [Examples](#examples)

---

## Quick Start

1. Create a new Class Library project targeting `net9.0-windows10.0.19041.0`
2. Reference `MacShell.Core` and `MacShell.UI` packages
3. Implement the `IWidget` interface
4. Create a `widget.json` manifest
5. Build and copy to `%LOCALAPPDATA%\MacShell\UserWidgets\YourWidget\`
6. MacShell will automatically detect and load your widget

---

## Widget Structure

```
YourWidget/
├── YourWidget.csproj
├── widget.json              # Required: Widget manifest
├── YourWidget.cs            # IWidget implementation
├── YourWidgetView.xaml      # Optional: XAML view
├── YourWidgetView.xaml.cs   # Optional: View code-behind
└── data/                    # Auto-created: Persistent storage
```

---

## Widget Manifest

Every widget must have a `widget.json` file in its root folder:

```json
{
  "id": "com.yourname.yourwidget",
  "name": "Your Widget",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "A brief description of your widget",
  "entryPoint": "YourNamespace.YourWidget",
  "assemblyName": "YourWidget.dll",
  "category": "System",
  "allowMultipleInstances": false,
  "icon": "IconName",
  "tags": ["tag1", "tag2"],
  "minMacShellVersion": "1.0.0",
  "settingsSchema": {
    "type": "object",
    "properties": {
      "settingName": {
        "type": "string",
        "default": "defaultValue",
        "description": "Description of this setting"
      }
    }
  },
  "defaultSettings": {
    "settingName": "defaultValue"
  }
}
```

### Manifest Fields

| Field | Required | Description |
|-------|----------|-------------|
| `id` | Yes | Unique identifier (reverse DNS format recommended) |
| `name` | Yes | Human-readable name |
| `version` | Yes | Semantic version (major.minor.patch) |
| `author` | Yes | Author name |
| `description` | Yes | Short description |
| `entryPoint` | Yes | Fully qualified type name implementing `IWidget` |
| `assemblyName` | Yes | DLL file name |
| `category` | No | Category for grouping (default: "General") |
| `allowMultipleInstances` | No | Allow multiple instances (default: false) |
| `icon` | No | Icon name from MacShell icon set |
| `tags` | No | Tags for search/filtering |
| `minMacShellVersion` | No | Minimum MacShell version required |
| `settingsSchema` | No | JSON Schema for settings validation |
| `defaultSettings` | No | Default settings values |

---

## Implementing IWidget

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;
using Microsoft.Extensions.Logging;

namespace YourNamespace;

public sealed class YourWidget : IWidget
{
    private readonly ILogger<YourWidget> _logger;
    private YourWidgetView? _view;
    private WidgetContext? _context;
    private YourWidgetSettings _settings = new();

    public WidgetManifest Manifest { get; } = new()
    {
        Id = "com.yourname.yourwidget",
        Name = "Your Widget",
        Version = "1.0.0",
        Author = "Your Name",
        Description = "A brief description",
        EntryPoint = "YourNamespace.YourWidget",
        AssemblyName = "YourWidget.dll",
        Category = "System"
    };

    public YourWidget(ILogger<YourWidget>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<YourWidget>.Instance;
    }

    public Task InitializeAsync(WidgetContext context)
    {
        _context = context;

        // Load settings
        if (_context.SettingsService != null)
        {
            var savedSettings = await _context.SettingsService.GetSettingsAsync(Manifest.Id);
            if (savedSettings != null)
            {
                _settings = savedSettings.Data.Deserialize<YourWidgetSettings>() ?? new YourWidgetSettings();
            }
        }

        // Create view
        _view = new YourWidgetView(this);
        _view.UpdateDisplay(_settings);

        // Start any timers, subscriptions, etc.
        _logger.LogInformation("Widget initialized");
        return Task.CompletedTask;
    }

    public FrameworkElement GetView()
    {
        return _view ?? new YourWidgetView(this);
    }

    public Task OnSettingsChangedAsync(WidgetSettings settings)
    {
        _settings = settings.Data.Deserialize<YourWidgetSettings>() ?? new YourWidgetSettings();
        _view?.UpdateDisplay(_settings);
        _logger.LogInformation("Settings updated");
        return Task.CompletedTask;
    }

    public Task ReloadAsync()
    {
        // Called during hot-reload
        _logger.LogInformation("Widget reloaded");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // Clean up timers, subscriptions, resources
        _view = null;
    }
}
```

---

## Widget Context & Services

The `WidgetContext` provides access to core MacShell services:

```csharp
public sealed class WidgetContext
{
    // System information (battery, network, time)
    public required ISystemService SystemService { get; init; }

    // Settings persistence
    public required IWidgetSettingsService SettingsService { get; init; }

    // Logging
    public required ILogger Logger { get; init; }

    // Per-widget data directory
    public required string WidgetDataPath { get; init; }

    // Unique instance ID
    public required string InstanceId { get; init; }
}
```

### Using SystemService

```csharp
// Subscribe to battery changes
_context.SystemService.BatteryChanged += (sender, battery) =>
{
    // Update UI with battery info
    _view.UpdateBattery(battery);
};

// Subscribe to network changes
_context.SystemService.NetworkChanged += (sender, network) =>
{
    _view.UpdateNetwork(network);
};

// Subscribe to time updates (every second)
_context.SystemService.TimeChanged += (sender, time) =>
{
    _view.UpdateTime(time);
};

// Start/stop monitoring
_context.SystemService.StartMonitoring();
_context.SystemService.StopMonitoring();
```

### Using SettingsService

```csharp
// Save settings
await _context.SettingsService.SaveSettingsAsync(Manifest.Id, new WidgetSettings
{
    WidgetId = Manifest.Id,
    Data = JsonSerializer.SerializeToElement(_settings)
});

// Settings changed event
_context.SettingsService.SettingsChanged += (sender, e) =>
{
    if (e.WidgetId == Manifest.Id)
    {
        // Handle settings change
    }
};
```

### Using Logger

```csharp
_logger.LogInformation("Widget started");
_logger.LogWarning("Something unexpected happened");
_logger.LogError(exception, "An error occurred");
```

### Using WidgetDataPath

```csharp
// Save data to widget's private folder
var dataFile = Path.Combine(_context.WidgetDataPath, "data.json");
await File.WriteAllTextAsync(dataFile, JsonSerializer.Serialize(myData));

// Read data
if (File.Exists(dataFile))
{
    var json = await File.ReadAllTextAsync(dataFile);
    myData = JsonSerializer.Deserialize<MyData>(json);
}
```

---

## Settings

### Settings Class

```csharp
public sealed class YourWidgetSettings
{
    public string SomeSetting { get; set; } = "default";
    public int NumberSetting { get; set; } = 42;
    public bool BooleanSetting { get; set; } = true;
}
```

### Serialization

Settings are stored as `JsonElement` for flexibility:

```csharp
// In OnSettingsChangedAsync
_settings = settings.Data.Deserialize<YourWidgetSettings>() ?? new YourWidgetSettings();

// When saving
var widgetSettings = new WidgetSettings
{
    WidgetId = Manifest.Id,
    Data = JsonSerializer.SerializeToElement(_settings)
};
await _context.SettingsService.SaveSettingsAsync(Manifest.Id, widgetSettings);
```

### Settings Schema

Define a JSON Schema in your manifest for validation and UI generation:

```json
"settingsSchema": {
  "type": "object",
  "properties": {
    "refreshInterval": {
      "type": "integer",
      "minimum": 1000,
      "maximum": 60000,
      "default": 5000,
      "description": "Refresh interval in milliseconds"
    },
    "theme": {
      "type": "string",
      "enum": ["light", "dark", "auto"],
      "default": "auto",
      "description": "Widget theme"
    }
  }
}
```

---

## UI Development

### XAML View

```xml
<UserControl
    x:Class="YourNamespace.YourWidgetView"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Background="Transparent"
    Padding="12,8">

    <StackPanel Orientation="Horizontal" Spacing="8">
        <FontIcon Glyph="&#xE7EE;" FontSize="16" Foreground="{ThemeResource MacShellAccentBrush}" />
        <StackPanel Spacing="2">
            <TextBlock x:Name="TitleText" FontSize="13" FontWeight="Medium" Foreground="{ThemeResource MacShellTextPrimaryBrush}" />
            <TextBlock x:Name="DetailText" FontSize="11" Foreground="{ThemeResource MacShellTextSecondaryBrush}" />
        </StackPanel>
    </StackPanel>
</UserControl>
```

### Code-Behind

```csharp
public sealed partial class YourWidgetView : UserControl
{
    public YourWidgetView()
    {
        InitializeComponent();
    }

    public void UpdateDisplay(YourWidgetSettings settings)
    {
        TitleText.Text = settings.SomeSetting;
        DetailText.Text = $"Value: {settings.NumberSetting}";
    }
}
```

### Using MacShell Theme Resources

```xml
<!-- Colors -->
Foreground="{ThemeResource MacShellTextPrimaryBrush}"
Foreground="{ThemeResource MacShellTextSecondaryBrush}"
Background="{ThemeResource MacShellSurfaceBrush}"
BorderBrush="{ThemeResource MacShellBorderBrush}"

<!-- Accent -->
Foreground="{ThemeResource MacShellAccentBrush}"

<!-- Spacing -->
Padding="{StaticResource MacShellSpacingM}"
Margin="{StaticResource MacShellSpacingS}"

<!-- Typography -->
Style="{StaticResource MacShellTextStyleBody}"
Style="{StaticResource MacShellTextStyleCaption}"

<!-- Shadows -->
Shadow="{StaticResource MacShellShadowMedium}"
```

---

## Building & Testing

### Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows10.0.19041.0</TargetFramework>
    <SupportedOSPlatformVersion>10.0.19041.0</SupportedOSPlatformVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\MacShell.Core\MacShell.Core.csproj" />
    <ProjectReference Include="..\..\MacShell.UI\MacShell.UI.csproj" />
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.241002001" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
    <PackageReference Include="CommunityToolkit.WinUI" Version="8.1.240916" />
  </ItemGroup>

  <ItemGroup>
    <Page Update="**\*.xaml" Generator="MSBuild:Compile" />
  </ItemGroup>

  <ItemGroup>
    <None Update="widget.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

### Build Commands

```bash
# Build widget
dotnet build YourWidget.csproj -c Release

# Output will be in bin/Release/net9.0-windows10.0.19041.0/
```

### Testing

1. Build your widget
2. Create folder: `%LOCALAPPDATA%\MacShell\UserWidgets\YourWidget\`
3. Copy `YourWidget.dll` and `widget.json` to that folder
4. MacShell will auto-detect and load it

---

## Hot Reload

MacShell automatically watches the UserWidgets folder for changes:

1. Modify your widget code
2. Rebuild (`dotnet build`)
3. MacShell detects the file change
4. Automatically unloads old version and loads new one
5. `ReloadAsync()` is called on your widget

### Tips for Hot Reload

- Keep `ReloadAsync()` fast
- Clean up resources in `Dispose()`
- Re-initialize subscriptions in `ReloadAsync()`
- Use `WidgetDataPath` for persistent data (survives reload)

---

## Distribution

### Sharing Your Widget

1. Create a ZIP file with:
   - `YourWidget.dll`
   - `widget.json`
   - Any additional dependencies

2. Users extract to `%LOCALAPPDATA%\MacShell\UserWidgets\YourWidget\`

3. Or create an installer that copies to the right location

### Widget Repository

Consider publishing to a widget repository (future feature):
- GitHub releases
- Custom widget store
- Package manager integration

---

## Examples

### Example 1: Simple Text Widget

```csharp
// SimpleWidget.cs
public sealed class SimpleWidget : IWidget
{
    public WidgetManifest Manifest { get; } = new()
    {
        Id = "com.example.simple",
        Name = "Simple Widget",
        Version = "1.0.0",
        Author = "Example",
        Description = "Shows static text",
        EntryPoint = "SimpleWidget",
        AssemblyName = "SimpleWidget.dll"
    };

    public Task InitializeAsync(WidgetContext context)
    {
        _view = new TextBlock
        {
            Text = "Hello, MacShell!",
            FontSize = 14,
            Foreground = Application.Current.Resources["MacShellTextPrimaryBrush"] as Brush
        };
        return Task.CompletedTask;
    }

    public FrameworkElement GetView() => _view;
    public Task OnSettingsChangedAsync(WidgetSettings settings) => Task.CompletedTask;
    public Task ReloadAsync() => Task.CompletedTask;
    public void Dispose() { }
}
```

### Example 2: Timer Widget

```csharp
// TimerWidget.cs
public sealed class TimerWidget : IWidget
{
    private DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public Task InitializeAsync(WidgetContext context)
    {
        _view = new TimerWidgetView();
        _timer.Tick += (s, e) => _view.UpdateTime(DateTime.Now);
        _timer.Start();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer.Stop();
    }
}
```

### Example 3: API Data Widget

```csharp
// ApiWidget.cs
public sealed class ApiWidget : IWidget
{
    private readonly HttpClient _http = new();

    public async Task InitializeAsync(WidgetContext context)
    {
        _view = new ApiWidgetView();
        await FetchDataAsync();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _timer.Tick += async (s, e) => await FetchDataAsync();
        _timer.Start();
    }

    private async Task FetchDataAsync()
    {
        try
        {
            var data = await _http.GetStringAsync("https://api.example.com/data");
            _view.UpdateData(data);
        }
        catch { }
    }
}
```

---

## Best Practices

1. **Always implement `IDisposable`** - Clean up timers, subscriptions, HttpClient
2. **Use `WidgetDataPath`** - For persistent data that survives reloads
3. **Handle errors gracefully** - Don't crash the shell if your widget fails
4. **Keep UI lightweight** - Widgets run in the shell process
5. **Use theme resources** - Support both light/dark modes automatically
6. **Minimize allocations** - Avoid GC pressure in timer callbacks
7. **Test hot reload** - Ensure `ReloadAsync()` works correctly

---

## API Reference

### IWidget Interface

```csharp
public interface IWidget : IDisposable
{
    WidgetManifest Manifest { get; }
    Task InitializeAsync(WidgetContext context);
    FrameworkElement GetView();
    Task OnSettingsChangedAsync(WidgetSettings settings);
    Task ReloadAsync();
}
```

### WidgetManifest

```csharp
public sealed class WidgetManifest
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Version { get; init; }
    public string Author { get; init; }
    public string Description { get; init; }
    public string EntryPoint { get; init; }
    public string AssemblyName { get; init; }
    public object? SettingsSchema { get; init; }
    public object? DefaultSettings { get; init; }
    public string? MinMacShellVersion { get; init; }
    public string Category { get; init; }
    public bool AllowMultipleInstances { get; init; }
    public string? Icon { get; init; }
    public IReadOnlyList<string> Tags { get; init; }
}
```

### WidgetSettings

```csharp
public sealed class WidgetSettings
{
    public string WidgetId { get; init; }
    public string InstanceId { get; init; }
    public JsonElement Data { get; init; }
    public DateTime LastModified { get; init; }
    public int SchemaVersion { get; init; }
}
```

---

## Support

- **Documentation**: https://github.com/macshell/macshell/wiki
- **Issues**: https://github.com/macshell/macshell/issues
- **Discussions**: https://github.com/macshell/macshell/discussions

---

*Last updated: 2024*