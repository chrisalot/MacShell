using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MacShell.Core.Models;

/// <summary>
/// Widget manifest containing metadata about a widget.
/// This corresponds to the widget.json file.
/// </summary>
public sealed class WidgetManifest
{
    /// <summary>
    /// Unique identifier for the widget (reverse DNS format recommended).
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Semantic version (major.minor.patch).
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>
    /// Author name.
    /// </summary>
    [JsonPropertyName("author")]
    public required string Author { get; init; }

    /// <summary>
    /// Short description.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>
    /// Fully qualified type name of the widget entry point (implements IWidget).
    /// </summary>
    [JsonPropertyName("entryPoint")]
    public required string EntryPoint { get; init; }

    /// <summary>
    /// Assembly file name (e.g., "MyWidget.dll").
    /// </summary>
    [JsonPropertyName("assemblyName")]
    public required string AssemblyName { get; init; }

    /// <summary>
    /// JSON schema for widget settings (optional).
    /// </summary>
    [JsonPropertyName("settingsSchema")]
    public object? SettingsSchema { get; init; }

    /// <summary>
    /// Default settings values (optional).
    /// </summary>
    [JsonPropertyName("defaultSettings")]
    public object? DefaultSettings { get; init; }

    /// <summary>
    /// Minimum MacShell version required.
    /// </summary>
    [JsonPropertyName("minMacShellVersion")]
    public string? MinMacShellVersion { get; init; }

    /// <summary>
    /// Widget category for grouping in settings.
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; init; } = "General";

    /// <summary>
    /// Whether the widget supports multiple instances.
    /// </summary>
    [JsonPropertyName("allowMultipleInstances")]
    public bool AllowMultipleInstances { get; init; } = false;

    /// <summary>
    /// Icon name (from MacShell icon set) or path to custom icon.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    /// <summary>
    /// Tags for searching/filtering.
    /// </summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// License information.
    /// </summary>
    [JsonPropertyName("license")]
    public string? License { get; init; }

    /// <summary>
    /// Repository URL.
    /// </summary>
    [JsonPropertyName("repository")]
    public string? Repository { get; init; }

    /// <summary>
    /// Creates a manifest from a JSON file.
    /// </summary>
    public static WidgetManifest FromJson(string json)
    {
        return System.Text.Json.JsonSerializer.Deserialize<WidgetManifest>(json, WidgetJsonContext.Default.WidgetManifest)
            ?? throw new InvalidOperationException("Failed to deserialize widget manifest");
    }

    /// <summary>
    /// Serializes the manifest to JSON.
    /// </summary>
    public string ToJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(this, WidgetJsonContext.Default.WidgetManifest);
    }
}

/// <summary>
/// JSON serialization context for WidgetManifest (source-generated).
/// </summary>
[JsonSerializable(typeof(WidgetManifest))]
[JsonSerializable(typeof(WidgetSettings))]
[JsonSerializable(typeof(DockItem))]
internal partial class WidgetJsonContext : JsonSerializerContext
{
}