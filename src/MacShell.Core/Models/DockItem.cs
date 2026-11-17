using System;
using System.Text.Json.Serialization;
using MacShell.Core.Interfaces;

namespace MacShell.Core.Models;

/// <summary>
/// Concrete implementation of IDockItem.
/// </summary>
public sealed class DockItem : IDockItem, IEquatable<DockItem>
{
    /// <inheritdoc />
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("executablePath")]
    public required string ExecutablePath { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;

    /// <inheritdoc />
    [JsonPropertyName("workingDirectory")]
    public required string WorkingDirectory { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("iconPath")]
    public required string IconPath { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("isPinned")]
    public bool IsPinned { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("isRunning")]
    public bool IsRunning { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("processId")]
    public int? ProcessId { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("windowHandle")]
    public nint WindowHandle { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("order")]
    public int Order { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("appUserModelId")]
    public string? AppUserModelId { get; init; }

    /// <inheritdoc />
    [JsonPropertyName("isStoreApp")]
    public bool IsStoreApp { get; init; }

    /// <summary>
    /// Last known window title (for running apps).
    /// </summary>
    [JsonIgnore]
    public string? WindowTitle { get; set; }

    /// <summary>
    /// Whether this item was auto-added (running but not pinned).
    /// </summary>
    [JsonIgnore]
    public bool IsAutoAdded => !IsPinned && IsRunning;

    /// <inheritdoc />
    public IDockItem Clone()
    {
        return new DockItem
        {
            Id = Id,
            Name = Name,
            ExecutablePath = ExecutablePath,
            Arguments = Arguments,
            WorkingDirectory = WorkingDirectory,
            IconPath = IconPath,
            IsPinned = IsPinned,
            IsRunning = IsRunning,
            ProcessId = ProcessId,
            WindowHandle = WindowHandle,
            Order = Order,
            AppUserModelId = AppUserModelId,
            IsStoreApp = IsStoreApp,
            WindowTitle = WindowTitle
        };
    }

    /// <summary>
    /// Creates a dock item for a pinned application.
    /// </summary>
    public static DockItem CreatePinned(string id, string name, string executablePath, string iconPath, string? arguments = null, string? workingDirectory = null, int order = 0)
    {
        return new DockItem
        {
            Id = id,
            Name = name,
            ExecutablePath = executablePath,
            Arguments = arguments ?? string.Empty,
            WorkingDirectory = workingDirectory ?? System.IO.Path.GetDirectoryName(executablePath) ?? string.Empty,
            IconPath = iconPath,
            IsPinned = true,
            IsRunning = false,
            Order = order
        };
    }

    /// <summary>
    /// Creates a dock item for a running application.
    /// </summary>
    public static DockItem CreateRunning(string id, string name, string executablePath, string iconPath, int processId, nint windowHandle, string? appUserModelId = null, bool isStoreApp = false, int order = 0)
    {
        return new DockItem
        {
            Id = id,
            Name = name,
            ExecutablePath = executablePath,
            Arguments = string.Empty,
            WorkingDirectory = System.IO.Path.GetDirectoryName(executablePath) ?? string.Empty,
            IconPath = iconPath,
            IsPinned = false,
            IsRunning = true,
            ProcessId = processId,
            WindowHandle = windowHandle,
            AppUserModelId = appUserModelId,
            IsStoreApp = isStoreApp,
            Order = order
        };
    }

    public bool Equals(DockItem? other)
    {
        if (other is null) return false;
        return Id == other.Id;
    }

    public override bool Equals(object? obj) => Equals(obj as DockItem);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(DockItem? left, DockItem? right) => Equals(left, right);
    public static bool operator !=(DockItem? left, DockItem? right) => !Equals(left, right);
}