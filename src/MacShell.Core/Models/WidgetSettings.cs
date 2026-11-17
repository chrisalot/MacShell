using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MacShell.Core.Models;

/// <summary>
/// Widget settings container. Uses JsonElement for flexible schema-less storage.
/// </summary>
public sealed record WidgetSettings
{
    /// <summary>
    /// Widget ID these settings belong to.
    /// </summary>
    [JsonPropertyName("widgetId")]
    public required string WidgetId { get; init; }

    /// <summary>
    /// Instance ID (for widgets that allow multiple instances).
    /// </summary>
    [JsonPropertyName("instanceId")]
    public string InstanceId { get; init; } = "default";

    /// <summary>
    /// Settings data as JSON element (flexible schema).
    /// </summary>
    [JsonPropertyName("data")]
    public JsonElement Data { get; init; } = JsonDocument.Parse("{}").RootElement;

    /// <summary>
    /// When the settings were last modified.
    /// </summary>
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Settings schema version (for migrations).
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = 1;

    /// <summary>
    /// Creates empty settings for a widget.
    /// </summary>
    public static WidgetSettings CreateDefault(string widgetId, string instanceId = "default")
    {
        return new WidgetSettings
        {
            WidgetId = widgetId,
            InstanceId = instanceId,
            Data = JsonDocument.Parse("{}").RootElement,
            LastModified = DateTime.UtcNow,
            SchemaVersion = 1
        };
    }

    /// <summary>
    /// Creates settings from a JSON string.
    /// </summary>
    public static WidgetSettings FromJson(string widgetId, string instanceId, string json)
    {
        var doc = JsonDocument.Parse(json);
        return new WidgetSettings
        {
            WidgetId = widgetId,
            InstanceId = instanceId,
            Data = doc.RootElement.Clone(),
            LastModified = DateTime.UtcNow,
            SchemaVersion = 1
        };
    }

    /// <summary>
    /// Gets a typed value from settings.
    /// </summary>
    public T? GetValue<T>(string key, T? defaultValue = default)
    {
        if (Data.ValueKind == JsonValueKind.Undefined || Data.ValueKind == JsonValueKind.Null)
            return defaultValue;

        if (Data.TryGetProperty(key, out var element))
        {
            try
            {
                return element.Deserialize<T>();
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Sets a value in settings (returns new instance).
    /// </summary>
    public WidgetSettings SetValue<T>(string key, T value)
    {
        var dict = new Dictionary<string, object?>();

        if (Data.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in Data.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.Clone();
            }
        }

        dict[key] = value;

        var json = JsonSerializer.Serialize(dict);
        var newSettings = FromJson(WidgetId, InstanceId, json);
        return newSettings with { LastModified = DateTime.UtcNow };
    }

    /// <summary>
    /// Removes a key from settings (returns new instance).
    /// </summary>
    public WidgetSettings RemoveValue(string key)
    {
        if (Data.ValueKind != JsonValueKind.Object)
            return this;

        var dict = new Dictionary<string, object?>();
        foreach (var prop in Data.EnumerateObject())
        {
            if (prop.Name != key)
                dict[prop.Name] = prop.Value.Clone();
        }

        var json = JsonSerializer.Serialize(dict);
        var newSettings = FromJson(WidgetId, InstanceId, json);
        return newSettings with { LastModified = DateTime.UtcNow };
    }

    /// <summary>
    /// Serializes settings to JSON string.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}