using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading.Tasks;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;
using MacShell.Core.Extensibility;
using Microsoft.Extensions.Logging;

namespace MacShell.Core.Services;

/// <summary>
/// Manages widget discovery, loading, and hot-reload.
/// </summary>
public sealed class WidgetManager : IDisposable
{
    private readonly ILogger<WidgetManager> _logger;
    private readonly IWidgetSettingsService _settingsService;
    private readonly ISystemService _systemService;
    private readonly string _userWidgetsPath;
    private readonly ConcurrentDictionary<string, LoadedWidget> _loadedWidgets = new();
    private readonly WidgetFileWatcher _fileWatcher;
    private bool _disposed;

    public WidgetManager(
        ILogger<WidgetManager> logger,
        IWidgetSettingsService settingsService,
        ISystemService systemService,
        string? userWidgetsPath = null)
    {
        _logger = logger;
        _settingsService = settingsService;
        _systemService = systemService;
        _userWidgetsPath = userWidgetsPath ?? GetDefaultUserWidgetsPath();

        _fileWatcher = new WidgetFileWatcher(_userWidgetsPath);
        _fileWatcher.WidgetChanged += OnWidgetFileChanged;
    }

    /// <summary>
    /// Event raised when a widget is loaded.
    /// </summary>
    public event EventHandler<IWidget>? WidgetLoaded;

    /// <summary>
    /// Event raised when a widget is unloaded.
    /// </summary>
    public event EventHandler<string>? WidgetUnloaded;

    /// <summary>
    /// Event raised when a widget fails to load.
    /// </summary>
    public event EventHandler<WidgetLoadErrorEventArgs>? WidgetLoadError;

    /// <summary>
    /// Gets all currently loaded widgets.
    /// </summary>
    public IReadOnlyDictionary<string, IWidget> LoadedWidgets => _loadedWidgets.ToDictionary(k => k.Key, v => v.Value.Widget);

    /// <summary>
    /// Discovers and loads all widgets from the user widgets folder.
    /// </summary>
    public async Task DiscoverAndLoadAsync()
    {
        if (!Directory.Exists(_userWidgetsPath))
        {
            Directory.CreateDirectory(_userWidgetsPath);
            CreateReadmeFile();
            _logger.LogInformation("Created user widgets directory at {Path}", _userWidgetsPath);
            return;
        }

        var widgetFolders = Directory.GetDirectories(_userWidgetsPath);
        foreach (var folder in widgetFolders)
        {
            await TryLoadWidgetFromFolderAsync(folder);
        }

        _fileWatcher.Start();
        _logger.LogInformation("Discovered and loaded {Count} widgets", _loadedWidgets.Count);
    }

    /// <summary>
    /// Loads a widget from a specific folder.
    /// </summary>
    public async Task<IWidget?> LoadWidgetAsync(string widgetFolderPath)
    {
        return await TryLoadWidgetFromFolderAsync(widgetFolderPath);
    }

    /// <summary>
    /// Unloads a widget by ID.
    /// </summary>
    public async Task UnloadWidgetAsync(string widgetId)
    {
        if (_loadedWidgets.TryRemove(widgetId, out var loaded))
        {
            try
            {
                await loaded.Widget.DisposeAsync();
                loaded.Context.Unload();
                _logger.LogInformation("Unloaded widget {WidgetId}", widgetId);
                WidgetUnloaded?.Invoke(this, widgetId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading widget {WidgetId}", widgetId);
            }
        }
    }

    /// <summary>
    /// Reloads a widget (for hot-reload).
    /// </summary>
    public async Task ReloadWidgetAsync(string widgetId)
    {
        if (_loadedWidgets.TryGetValue(widgetId, out var loaded))
        {
            var folderPath = loaded.FolderPath;
            await UnloadWidgetAsync(widgetId);
            await TryLoadWidgetFromFolderAsync(folderPath);
        }
    }

    /// <summary>
    /// Gets a loaded widget by ID.
    /// </summary>
    public IWidget? GetWidget(string widgetId)
    {
        return _loadedWidgets.TryGetValue(widgetId, out var loaded) ? loaded.Widget : null;
    }

    private async Task<IWidget?> TryLoadWidgetFromFolderAsync(string folderPath)
    {
        try
        {
            var manifestPath = Path.Combine(folderPath, "widget.json");
            if (!File.Exists(manifestPath))
            {
                _logger.LogWarning("No widget.json found in {Folder}", folderPath);
                return null;
            }

            var manifestJson = await File.ReadAllTextAsync(manifestPath);
            var manifest = WidgetManifest.FromJson(manifestJson);

            var assemblyPath = Path.Combine(folderPath, manifest.AssemblyName);
            if (!File.Exists(assemblyPath))
            {
                _logger.LogWarning("Assembly {Assembly} not found for widget {WidgetId}", manifest.AssemblyName, manifest.Id);
                return null;
            }

            // Check if already loaded
            if (_loadedWidgets.ContainsKey(manifest.Id))
            {
                _logger.LogDebug("Widget {WidgetId} already loaded", manifest.Id);
                return _loadedWidgets[manifest.Id].Widget;
            }

            // Create isolated load context
            var context = new PluginAssemblyContext(manifest.Id, assemblyPath, folderPath);
            var assembly = context.LoadFromAssemblyPath(assemblyPath);

            // Find and instantiate widget type
            var widgetType = assembly.GetType(manifest.EntryPoint);
            if (widgetType == null)
            {
                throw new InvalidOperationException($"Widget entry point type '{manifest.EntryPoint}' not found in assembly");
            }

            if (!typeof(IWidget).IsAssignableFrom(widgetType))
            {
                throw new InvalidOperationException($"Type '{manifest.EntryPoint}' does not implement IWidget");
            }

            var widget = (IWidget)Activator.CreateInstance(widgetType)!;

            // Create widget context
            var widgetDataPath = Path.Combine(folderPath, "data");
            Directory.CreateDirectory(widgetDataPath);

            var instanceId = Guid.NewGuid().ToString();
            var settings = await _settingsService.GetSettingsAsync(manifest.Id) ?? WidgetSettings.CreateDefault(manifest.Id);

            var widgetContext = new WidgetContext
            {
                SystemService = _systemService,
                SettingsService = _settingsService,
                Logger = _logger,
                WidgetDataPath = widgetDataPath,
                InstanceId = instanceId
            };

            // Initialize widget
            await widget.InitializeAsync(widgetContext);

            // Subscribe to settings changes
            _settingsService.SettingsChanged += async (s, e) =>
            {
                if (e.WidgetId == manifest.Id)
                {
                    await widget.OnSettingsChangedAsync(e.Settings);
                }
            };

            var loadedWidget = new LoadedWidget
            {
                Widget = widget,
                Context = context,
                Manifest = manifest,
                FolderPath = folderPath,
                InstanceId = instanceId
            };

            _loadedWidgets[manifest.Id] = loadedWidget;

            _logger.LogInformation("Loaded widget {WidgetId} v{Version} from {Path}", manifest.Id, manifest.Version, folderPath);
            WidgetLoaded?.Invoke(this, widget);

            return widget;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load widget from {Folder}", folderPath);
            WidgetLoadError?.Invoke(this, new WidgetLoadErrorEventArgs { FolderPath = folderPath, Error = ex.Message });
            return null;
        }
    }

    private async void OnWidgetFileChanged(object? sender, WidgetFileChangedEventArgs e)
    {
        _logger.LogInformation("Widget file changed: {Path} ({ChangeType})", e.FullPath, e.ChangeType);

        try
        {
            // Find which widget this belongs to
            var widgetId = _loadedWidgets.Values.FirstOrDefault(w => w.FolderPath == e.FolderPath)?.Manifest.Id;
            if (widgetId != null)
            {
                await ReloadWidgetAsync(widgetId);
            }
            else
            {
                // New widget added
                await TryLoadWidgetFromFolderAsync(e.FolderPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling widget file change");
        }
    }

    private static string GetDefaultUserWidgetsPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "MacShell", "UserWidgets");
    }

    private void CreateReadmeFile()
    {
        var readmePath = Path.Combine(_userWidgetsPath, "README.md");
        var readmeContent = @"# MacShell User Widgets

Place your custom widgets in this folder. Each widget should be in its own subfolder with:

```
MyWidget/
├── widget.json          # Widget manifest
├── MyWidget.dll         # Compiled widget assembly
└── data/                # Widget data (auto-created)
```

## Widget Manifest (widget.json)

```json
{
  ""id"": ""com.example.mywidget"",
  ""name"": ""My Widget"",
  ""version"": ""1.0.0"",
  ""author"": ""Your Name"",
  ""description"": ""A custom widget"",
  ""entryPoint"": ""MyWidget.MyWidget"",
  ""assemblyName"": ""MyWidget.dll"",
  ""category"": ""System"",
  ""allowMultipleInstances"": false
}
```

## Creating a Widget

1. Create a new Class Library project targeting `net9.0-windows10.0.19041.0`
2. Reference `MacShell.Core` package
3. Implement `IWidget` interface
4. Build and copy the DLL + widget.json to a subfolder here
5. MacShell will auto-detect and load it (hot-reload supported!)

## Example

See the built-in widgets in the MacShell installation folder for reference.
";
        File.WriteAllText(readmePath, readmeContent);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _fileWatcher.Dispose();

        foreach (var (id, loaded) in _loadedWidgets)
        {
            try
            {
                loaded.Widget.Dispose();
                loaded.Context.Unload();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing widget {WidgetId}", id);
            }
        }
        _loadedWidgets.Clear();
    }

    private sealed class LoadedWidget
    {
        public required IWidget Widget { get; init; }
        public required PluginAssemblyContext Context { get; init; }
        public required WidgetManifest Manifest { get; init; }
        public required string FolderPath { get; init; }
        public required string InstanceId { get; init; }
    }
}

/// <summary>
/// Event arguments for widget load errors.
/// </summary>
public sealed class WidgetLoadErrorEventArgs : EventArgs
{
    public required string FolderPath { get; init; }
    public required string Error { get; init; }
}