using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;
using Microsoft.Extensions.Logging;

namespace MacShell.Core.Services;

/// <summary>
/// Widget settings persistence service.
/// </summary>
public sealed class WidgetSettingsService : IWidgetSettingsService
{
    private readonly ILogger<WidgetSettingsService> _logger;
    private readonly string _settingsPath;
    private readonly Dictionary<string, WidgetSettings> _cache = new();
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private bool _loaded;

    public WidgetSettingsService(ILogger<WidgetSettingsService> logger, string? settingsPath = null)
    {
        _logger = logger;
        _settingsPath = settingsPath ?? GetDefaultSettingsPath();
    }

    public event EventHandler<WidgetSettingsChangedEventArgs>? SettingsChanged;

    public async Task<WidgetSettings?> GetSettingsAsync(string widgetId)
    {
        await EnsureLoadedAsync();

        if (_cache.TryGetValue(widgetId, out var settings))
        {
            return settings;
        }

        // Return default settings if not found
        return WidgetSettings.CreateDefault(widgetId);
    }

    public async Task SaveSettingsAsync(string widgetId, WidgetSettings settings)
    {
        await EnsureLoadedAsync();

        settings = settings with { LastModified = DateTime.UtcNow };
        _cache[widgetId] = settings;

        await SaveAsync();

        SettingsChanged?.Invoke(this, new WidgetSettingsChangedEventArgs
        {
            WidgetId = widgetId,
            Settings = settings
        });
    }

    public async Task ResetSettingsAsync(string widgetId)
    {
        await EnsureLoadedAsync();

        if (_cache.Remove(widgetId))
        {
            await SaveAsync();

            SettingsChanged?.Invoke(this, new WidgetSettingsChangedEventArgs
            {
                WidgetId = widgetId,
                Settings = WidgetSettings.CreateDefault(widgetId)
            });
        }
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;

        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                var allSettings = JsonSerializer.Deserialize<Dictionary<string, WidgetSettings>>(json, SettingsJsonContext.Default.DictionaryStringWidgetSettings);

                if (allSettings != null)
                {
                    foreach (var kvp in allSettings)
                    {
                        _cache[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load widget settings, starting fresh");
        }

        _loaded = true;
    }

    private async Task SaveAsync()
    {
        await _saveLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_cache, SettingsJsonContext.Default.DictionaryStringWidgetSettings);
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private static string GetDefaultSettingsPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "MacShell", "widget-settings.json");
    }
}

/// <summary>
/// JSON serialization context for WidgetSettings.
/// </summary>
[System.Text.Json.Serialization.JsonSerializable(typeof(Dictionary<string, WidgetSettings>))]
[System.Text.Json.Serialization.JsonSerializable(typeof(WidgetSettings))]
internal partial class SettingsJsonContext : System.Text.Json.Serialization.JsonSerializerContext
{
}