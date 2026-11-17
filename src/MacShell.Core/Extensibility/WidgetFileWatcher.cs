using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace MacShell.Core.Extensibility;

/// <summary>
/// File system watcher for widget folders with debouncing.
/// </summary>
public sealed class WidgetFileWatcher : IDisposable
{
    private readonly string _watchPath;
    private readonly ILogger<WidgetFileWatcher> _logger;
    private readonly FileSystemWatcher _watcher;
    private readonly System.Threading.Timer _debounceTimer;
    private readonly object _lock = new();
    private string? _pendingFolder;
    private WidgetFileChangedEventArgs? _pendingArgs;
    private bool _disposed;

    public WidgetFileWatcher(string watchPath, ILogger<WidgetFileWatcher>? logger = null)
    {
        _watchPath = watchPath;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<WidgetFileWatcher>.Instance;

        if (!Directory.Exists(_watchPath))
        {
            Directory.CreateDirectory(_watchPath);
        }

        _watcher = new FileSystemWatcher(_watchPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = false
        };

        _watcher.Created += OnFileChanged;
        _watcher.Changed += OnFileChanged;
        _watcher.Deleted += OnFileChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;

        _debounceTimer = new System.Threading.Timer(ProcessPendingChange, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Event raised when a widget file changes.
    /// </summary>
    public event EventHandler<WidgetFileChangedEventArgs>? WidgetChanged;

    /// <summary>
    /// Starts watching for changes.
    /// </summary>
    public void Start()
    {
        _watcher.EnableRaisingEvents = true;
        _logger.LogInformation("Started watching {Path} for widget changes", _watchPath);
    }

    /// <summary>
    /// Stops watching for changes.
    /// </summary>
    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
        _logger.LogInformation("Stopped watching {Path}", _watchPath);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Only care about widget.json and DLL files
        var ext = Path.GetExtension(e.Name).ToLowerInvariant();
        if (ext != ".json" && ext != ".dll" && ext != ".pdb")
            return;

        var folderPath = GetWidgetFolderPath(e.FullPath);
        if (string.IsNullOrEmpty(folderPath))
            return;

        lock (_lock)
        {
            _pendingFolder = folderPath;
            _pendingArgs = new WidgetFileChangedEventArgs
            {
                FullPath = e.FullPath,
                FolderPath = folderPath,
                ChangeType = e.ChangeType,
                Name = e.Name
            };
            _debounceTimer.Change(500, Timeout.Infinite); // 500ms debounce
        }
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        var folderPath = GetWidgetFolderPath(e.FullPath);
        if (string.IsNullOrEmpty(folderPath))
            return;

        lock (_lock)
        {
            _pendingFolder = folderPath;
            _pendingArgs = new WidgetFileChangedEventArgs
            {
                FullPath = e.FullPath,
                FolderPath = folderPath,
                ChangeType = WatcherChangeTypes.Renamed,
                Name = e.Name,
                OldName = e.OldName
            };
            _debounceTimer.Change(500, Timeout.Infinite);
        }
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "FileSystemWatcher error");
    }

    private void ProcessPendingChange(object? state)
    {
        WidgetFileChangedEventArgs? args;
        lock (_lock)
        {
            args = _pendingArgs;
            _pendingArgs = null;
            _pendingFolder = null;
        }

        if (args != null)
        {
            try
            {
                WidgetChanged?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error raising WidgetChanged event");
            }
        }
    }

    private string? GetWidgetFolderPath(string fullPath)
    {
        try
        {
            var relative = Path.GetRelativePath(_watchPath, fullPath);
            var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parts.Length > 0)
            {
                return Path.Combine(_watchPath, parts[0]);
            }
        }
        catch
        {
            // Ignore path errors
        }
        return null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnFileChanged;
        _watcher.Changed -= OnFileChanged;
        _watcher.Deleted -= OnFileChanged;
        _watcher.Renamed -= OnRenamed;
        _watcher.Error -= OnError;
        _watcher.Dispose();

        _debounceTimer.Dispose();
    }
}

/// <summary>
/// Event arguments for widget file changes.
/// </summary>
public sealed class WidgetFileChangedEventArgs : EventArgs
{
    public required string FullPath { get; init; }
    public required string FolderPath { get; init; }
    public required WatcherChangeTypes ChangeType { get; init; }
    public required string Name { get; init; }
    public string? OldName { get; init; }
}