using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MacShell.Core.Models;

namespace MacShell.Core.Interfaces;

/// <summary>
/// Application launcher service for searching and launching installed applications.
/// </summary>
public interface IAppLauncher
{
    /// <summary>
    /// Searches for applications matching the query.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <returns>List of matching applications.</returns>
    Task<IReadOnlyList<AppInfo>> SearchAsync(string query, int maxResults = 20);

    /// <summary>
    /// Launches an application.
    /// </summary>
    /// <param name="app">Application to launch.</param>
    /// <param name="arguments">Optional launch arguments.</param>
    /// <returns>True if launch was successful.</returns>
    Task<bool> LaunchAsync(AppInfo app, string? arguments = null);

    /// <summary>
    /// Launches an application by executable path.
    /// </summary>
    /// <param name="executablePath">Path to executable.</param>
    /// <param name="arguments">Optional launch arguments.</param>
    /// <returns>True if launch was successful.</returns>
    Task<bool> LaunchByPathAsync(string executablePath, string? arguments = null);

    /// <summary>
    /// Refreshes the application index.
    /// </summary>
    Task RefreshIndexAsync();

    /// <summary>
    /// Event raised when the application index is updated.
    /// </summary>
    event EventHandler IndexUpdated;
}

/// <summary>
/// Information about an installed application.
/// </summary>
public sealed record AppInfo
{
    /// <summary>
    /// Unique identifier for this application.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Path to the executable.
    /// </summary>
    public required string ExecutablePath { get; init; }

    /// <summary>
    /// Arguments to pass when launching (if any).
    /// </summary>
    public string? DefaultArguments { get; init; }

    /// <summary>
    /// Working directory.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Path to the application icon.
    /// </summary>
    public string? IconPath { get; init; }

    /// <summary>
    /// Application User Model ID (for Store apps).
    /// </summary>
    public string? AppUserModelId { get; init; }

    /// <summary>
    /// Whether this is a Store/UWP app.
    /// </summary>
    public bool IsStoreApp { get; init; }

    /// <summary>
    /// Publisher display name (for Store apps).
    /// </summary>
    public string? Publisher { get; init; }

    /// <summary>
    /// Application description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Keywords for search matching.
    /// </summary>
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates a dock item from this app info.
    /// </summary>
    public IDockItem ToDockItem() => new DockItem
    {
        Id = Id,
        Name = Name,
        ExecutablePath = ExecutablePath,
        Arguments = DefaultArguments ?? string.Empty,
        WorkingDirectory = WorkingDirectory ?? System.IO.Path.GetDirectoryName(ExecutablePath) ?? string.Empty,
        IconPath = IconPath ?? string.Empty,
        IsPinned = false,
        IsRunning = false,
        AppUserModelId = AppUserModelId,
        IsStoreApp = IsStoreApp
    };
}