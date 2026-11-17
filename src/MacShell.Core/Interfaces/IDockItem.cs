using System;

namespace MacShell.Core.Interfaces;

/// <summary>
/// Represents an item in the dock (pinned app or running app).
/// </summary>
public interface IDockItem
{
    /// <summary>
    /// Unique identifier for this dock item.
    /// </summary>
    string Id { get; set; }

    /// <summary>
    /// Display name of the application.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Path to the application executable.
    /// </summary>
    string ExecutablePath { get; set; }

    /// <summary>
    /// Arguments to pass when launching.
    /// </summary>
    string Arguments { get; set; }

    /// <summary>
    /// Working directory for the application.
    /// </summary>
    string WorkingDirectory { get; set; }

    /// <summary>
    /// Icon path or resource key.
    /// </summary>
    string IconPath { get; set; }

    /// <summary>
    /// Whether this item is pinned (persists across sessions).
    /// </summary>
    bool IsPinned { get; set; }

    /// <summary>
    /// Whether the application is currently running.
    /// </summary>
    bool IsRunning { get; set; }

    /// <summary>
    /// Process ID if running (null if not running).
    /// </summary>
    int? ProcessId { get; set; }

    /// <summary>
    /// Window handle if running (null if not running).
    /// </summary>
    nint WindowHandle { get; set; }

    /// <summary>
    /// Window title if running.
    /// </summary>
    string? WindowTitle { get; set; }

    /// <summary>
    /// Display order in the dock (lower = leftmost).
    /// </summary>
    int Order { get; set; }

    /// <summary>
    /// Application User Model ID (for Store apps).
    /// </summary>
    string? AppUserModelId { get; }

    /// <summary>
    /// Whether this is a Store/UWP app.
    /// </summary>
    bool IsStoreApp { get; }

    /// <summary>
    /// Creates a deep copy of this dock item.
    /// </summary>
    IDockItem Clone();
}

/// <summary>
/// Dock item factory for creating dock items from various sources.
/// </summary>
public interface IDockItemFactory
{
    /// <summary>
    /// Creates a dock item from an executable path.
    /// </summary>
    IDockItem CreateFromExecutable(string executablePath, string? arguments = null);

    /// <summary>
    /// Creates a dock item from an AppUserModelId (Store app).
    /// </summary>
    IDockItem CreateFromAppUserModelId(string appUserModelId);

    /// <summary>
    /// Creates a dock item for a running process.
    /// </summary>
    IDockItem CreateFromProcess(int processId);
}