using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;
using Microsoft.Extensions.Logging;

namespace MacShell.Core.Services;

/// <summary>
/// Manages dock items (pinned apps, running apps, order, persistence).
/// </summary>
public sealed class DockManager
{
    private readonly ILogger<DockManager> _logger;
    private readonly IDockItemFactory _dockItemFactory;
    private readonly string _settingsPath;
    private readonly ObservableCollection<IDockItem> _items = new();
    private readonly Dictionary<string, IDockItem> _itemLookup = new(StringComparer.OrdinalIgnoreCase);

    public DockManager(ILogger<DockManager> logger, IDockItemFactory dockItemFactory, string? settingsPath = null)
    {
        _logger = logger;
        _dockItemFactory = dockItemFactory;
        _settingsPath = settingsPath ?? GetDefaultSettingsPath();

        _items.CollectionChanged += (s, e) => ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event raised when the dock items collection changes.
    /// </summary>
    public event EventHandler? ItemsChanged;

    /// <summary>
    /// Event raised when a specific item's running state changes.
    /// </summary>
    public event EventHandler<DockItemRunningStateChangedEventArgs>? ItemRunningStateChanged;

    /// <summary>
    /// Gets all dock items in order.
    /// </summary>
    public IReadOnlyList<IDockItem> Items => _items.ToList();

    /// <summary>
    /// Gets only pinned items.
    /// </summary>
    public IReadOnlyList<IDockItem> PinnedItems => _items.Where(i => i.IsPinned).ToList();

    /// <summary>
    /// Gets only running (non-pinned) items.
    /// </summary>
    public IReadOnlyList<IDockItem> RunningItems => _items.Where(i => i.IsRunning && !i.IsPinned).ToList();

    /// <summary>
    /// Loads dock configuration from disk.
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                // Deserialize to concrete DockItem type for JSON compatibility
                var savedItems = JsonSerializer.Deserialize<List<DockItem>>(json, DockJsonContext.Default.ListDockItem);
                if (savedItems != null)
                {
                    foreach (var item in savedItems.OrderBy(i => i.Order))
                    {
                        AddItemInternal(item);
                    }
                    _logger.LogInformation("Loaded {Count} dock items from settings", _items.Count);
                }
            }
            else
            {
                // Add default pinned apps on first run
                AddDefaultPinnedApps();
                await SaveAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load dock settings");
            AddDefaultPinnedApps();
        }
    }

    /// <summary>
    /// Saves dock configuration to disk.
    /// </summary>
    public async Task SaveAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Only save pinned items (running items are transient)
            // Cast to DockItem for JSON serialization (all items are DockItem instances)
            var itemsToSave = _items.Where(i => i.IsPinned).Cast<DockItem>().ToList();
            var json = JsonSerializer.Serialize(itemsToSave, DockJsonContext.Default.ListDockItem);
            await File.WriteAllTextAsync(_settingsPath, json);
            _logger.LogDebug("Saved {Count} pinned dock items", itemsToSave.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save dock settings");
        }
    }

    /// <summary>
    /// Adds a pinned application to the dock.
    /// </summary>
    public IDockItem PinApplication(string executablePath, string? arguments = null, int? insertAt = null)
    {
        var item = _dockItemFactory.CreateFromExecutable(executablePath, arguments);
        item.IsPinned = true;
        item.IsRunning = false;

        // Check if already exists (by executable path)
        var existing = _items.FirstOrDefault(i =>
            string.Equals(i.ExecutablePath, executablePath, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.IsPinned = true;
            existing.Arguments = arguments ?? string.Empty;
            _logger.LogInformation("Updated pinned app: {Name}", existing.Name);
            return existing;
        }

        var order = insertAt ?? _items.Count;
        item.Order = order;
        AddItemInternal(item);
        ReorderItems();
        _ = SaveAsync();
        _logger.LogInformation("Pinned app: {Name}", item.Name);
        return item;
    }

    /// <summary>
    /// Removes a pinned application from the dock.
    /// </summary>
    public bool UnpinApplication(string itemId)
    {
        if (_itemLookup.TryGetValue(itemId, out var item))
        {
            if (item.IsPinned)
            {
                item.IsPinned = false;

                // If not running, remove entirely
                if (!item.IsRunning)
                {
                    RemoveItem(itemId);
                }
                else
                {
                    // Keep as running item, move to end
                    item.Order = _items.Count;
                    ReorderItems();
                }

                _ = SaveAsync();
                _logger.LogInformation("Unpinned app: {Name}", item.Name);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Updates the running state of an application.
    /// </summary>
    public void UpdateRunningState(int processId, nint windowHandle, bool isRunning, string? windowTitle = null)
    {
        // Find by process ID
        var item = _items.FirstOrDefault(i => i.ProcessId == processId);

        if (isRunning)
        {
            if (item == null)
            {
                // New running app - try to create from process
                try
                {
                    item = _dockItemFactory.CreateFromProcess(processId);
                    item.ProcessId = processId;
                    item.WindowHandle = windowHandle;
                    item.IsRunning = true;
                    item.WindowTitle = windowTitle;
                    AddItemInternal(item);
                }
                catch
                {
                    _logger.LogWarning("Could not create dock item for process {ProcessId}", processId);
                }
            }
            else
            {
                item.ProcessId = processId;
                item.WindowHandle = windowHandle;
                item.IsRunning = true;
                item.WindowTitle = windowTitle;

                var wasRunning = item.IsRunning;
                if (!wasRunning)
                {
                    ItemRunningStateChanged?.Invoke(this, new DockItemRunningStateChangedEventArgs { Item = item, IsRunning = true });
                }
            }
        }
        else
        {
            if (item != null)
            {
                var wasRunning = item.IsRunning;
                item.IsRunning = false;
                item.ProcessId = null;
                item.WindowHandle = 0;
                item.WindowTitle = null;

                if (wasRunning)
                {
                    ItemRunningStateChanged?.Invoke(this, new DockItemRunningStateChangedEventArgs { Item = item, IsRunning = false });
                }

                // If not pinned, remove after a delay (keep for animation)
                if (!item.IsPinned)
                {
                    // We'll let the UI handle removal timing
                }
            }
        }

        ReorderItems();
    }

    /// <summary>
    /// Activates (brings to front) the window for a dock item.
    /// </summary>
    public async Task<bool> ActivateItemAsync(string itemId)
    {
        if (_itemLookup.TryGetValue(itemId, out var item) && item.WindowHandle != 0)
        {
            // This would call into IWindowManager - placeholder for now
            _logger.LogDebug("Activate requested for {Name} (HWND: {Handle})", item.Name, item.WindowHandle);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Launches a pinned application.
    /// </summary>
    public async Task<bool> LaunchItemAsync(string itemId)
    {
        if (_itemLookup.TryGetValue(itemId, out var item))
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = item.ExecutablePath,
                    Arguments = item.Arguments,
                    WorkingDirectory = item.WorkingDirectory,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
                _logger.LogDebug("Launched {Name}", item.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to launch {Name}", item.Name);
            }
        }
        return false;
    }

    /// <summary>
    /// Reorders a dock item.
    /// </summary>
    public void ReorderItem(string itemId, int newIndex)
    {
        if (_itemLookup.TryGetValue(itemId, out var item))
        {
            var oldIndex = _items.IndexOf(item);
            if (oldIndex >= 0 && oldIndex != newIndex)
            {
                _items.Move(oldIndex, newIndex);
                ReorderItems();
                _ = SaveAsync();
            }
        }
    }

    /// <summary>
    /// Gets a dock item by ID.
    /// </summary>
    public IDockItem? GetItem(string itemId)
    {
        _itemLookup.TryGetValue(itemId, out var item);
        return item;
    }

    /// <summary>
    /// Gets a dock item by process ID.
    /// </summary>
    public IDockItem? GetItemByProcessId(int processId)
    {
        return _items.FirstOrDefault(i => i.ProcessId == processId);
    }

    /// <summary>
    /// Gets a dock item by window handle.
    /// </summary>
    public IDockItem? GetItemByWindowHandle(nint handle)
    {
        return _items.FirstOrDefault(i => i.WindowHandle == handle);
    }

    private void AddItemInternal(IDockItem item)
    {
        if (!_itemLookup.ContainsKey(item.Id))
        {
            _itemLookup[item.Id] = item;
            _items.Add(item);
        }
    }

    private void RemoveItem(string itemId)
    {
        if (_itemLookup.TryGetValue(itemId, out var item))
        {
            _items.Remove(item);
            _itemLookup.Remove(itemId);
        }
    }

    private void ReorderItems()
    {
        // Pinned items first (by order), then running items (by order)
        var ordered = _items
            .OrderBy(i => i.IsPinned ? 0 : 1)
            .ThenBy(i => i.Order)
            .ToList();

        for (int i = 0; i < ordered.Count; i++)
        {
            ordered[i].Order = i;
            var currentIndex = _items.IndexOf(ordered[i]);
            if (currentIndex != i)
            {
                _items.Move(currentIndex, i);
            }
        }
    }

    private void AddDefaultPinnedApps()
    {
        // Add common Windows apps as defaults
        var defaultApps = new[]
        {
            ("File Explorer", @"C:\Windows\explorer.exe"),
            ("Settings", "ms-settings:"),
            ("Terminal", @"C:\Windows\System32\WindowsTerminal.exe"),
            ("Browser", @"C:\Program Files\Google\Chrome\Application\chrome.exe"),
        };

        foreach (var (name, path) in defaultApps)
        {
            if (File.Exists(path) || path.StartsWith("ms-"))
            {
                try
                {
                    PinApplication(path);
                }
                catch
                {
                    // Ignore if can't pin
                }
            }
        }
    }

    private static string GetDefaultSettingsPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "MacShell", "dock.json");
    }
}

/// <summary>
/// Event arguments for dock item running state changes.
/// </summary>
public sealed class DockItemRunningStateChangedEventArgs : EventArgs
{
    public required IDockItem Item { get; init; }
    public required bool IsRunning { get; init; }
}

/// <summary>
/// JSON serialization context for DockItem.
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(List<DockItem>))]
[System.Text.Json.Serialization.JsonSerializable(typeof(DockItem))]
internal partial class DockJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}