using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MacShell.Core.Models;

namespace MacShell.Core.Interfaces;

/// <summary>
/// Core widget contract. All widgets must implement this interface.
/// </summary>
public interface IWidget : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the widget manifest containing metadata.
    /// </summary>
    WidgetManifest Manifest { get; }

    /// <summary>
    /// Initializes the widget with the provided context.
    /// Called once when the widget is first loaded.
    /// </summary>
    /// <param name="context">Widget context providing access to core services.</param>
    Task InitializeAsync(WidgetContext context);

    /// <summary>
    /// Gets the UI element representing the widget's view.
    /// </summary>
    /// <returns>The widget's root view as an opaque object (FrameworkElement in WinUI).</returns>
    object GetView();

    /// <summary>
    /// Called when widget settings are changed by the user.
    /// </summary>
    /// <param name="settings">The new settings.</param>
    Task OnSettingsChangedAsync(WidgetSettings settings);

    /// <summary>
    /// Called when the widget should reload (e.g., after hot-reload).
    /// </summary>
    Task ReloadAsync();

    /// <summary>
    /// Asynchronously disposes the widget.
    /// </summary>
    ValueTask IAsyncDisposable.DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Context provided to widgets during initialization.
/// </summary>
public sealed class WidgetContext
{
    /// <summary>
    /// System service for battery, network, time, etc.
    /// </summary>
    public required ISystemService SystemService { get; init; }

    /// <summary>
    /// Service for reading/writing widget settings.
    /// </summary>
    public required IWidgetSettingsService SettingsService { get; init; }

    /// <summary>
    /// Logger for the widget.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    /// Path to the widget's data directory (for persistent storage).
    /// </summary>
    public required string WidgetDataPath { get; init; }

    /// <summary>
    /// Unique identifier for this widget instance.
    /// </summary>
    public required string InstanceId { get; init; }
}

/// <summary>
/// Widget settings service interface.
/// </summary>
public interface IWidgetSettingsService
{
    /// <summary>
    /// Gets the current settings for a widget.
    /// </summary>
    Task<WidgetSettings?> GetSettingsAsync(string widgetId);

    /// <summary>
    /// Saves settings for a widget.
    /// </summary>
    Task SaveSettingsAsync(string widgetId, WidgetSettings settings);

    /// <summary>
    /// Resets settings to defaults.
    /// </summary>
    Task ResetSettingsAsync(string widgetId);

    /// <summary>
    /// Event raised when settings change.
    /// </summary>
    event EventHandler<WidgetSettingsChangedEventArgs> SettingsChanged;
}

/// <summary>
/// Event arguments for widget settings changes.
/// </summary>
public sealed class WidgetSettingsChangedEventArgs : EventArgs
{
    public required string WidgetId { get; init; }
    public required WidgetSettings Settings { get; init; }
}