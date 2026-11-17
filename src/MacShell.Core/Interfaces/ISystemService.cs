using System;

namespace MacShell.Core.Interfaces;

/// <summary>
/// Provides access to system information (battery, network, time, etc.).
/// All events are raised on the UI thread.
/// </summary>
public interface ISystemService
{
    /// <summary>
    /// Current battery information.
    /// </summary>
    BatteryInfo Battery { get; }

    /// <summary>
    /// Current network information.
    /// </summary>
    NetworkInfo Network { get; }

    /// <summary>
    /// Current date/time.
    /// </summary>
    DateTime CurrentTime { get; }

    /// <summary>
    /// Time zone information.
    /// </summary>
    TimeZoneInfo TimeZone { get; }

    /// <summary>
    /// Event raised when battery status changes.
    /// </summary>
    event EventHandler<BatteryInfo> BatteryChanged;

    /// <summary>
    /// Event raised when network status changes.
    /// </summary>
    event EventHandler<NetworkInfo> NetworkChanged;

    /// <summary>
    /// Event raised every second for time updates.
    /// </summary>
    event EventHandler<DateTime> TimeChanged;

    /// <summary>
    /// Starts monitoring system events.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stops monitoring system events.
    /// </summary>
    void StopMonitoring();
}

/// <summary>
/// Battery information.
/// </summary>
public sealed record BatteryInfo
{
    /// <summary>
    /// Battery charge percentage (0-100). Null if no battery.
    /// </summary>
    public int? Percentage { get; init; }

    /// <summary>
    /// Current battery status.
    /// </summary>
    public BatteryStatus Status { get; init; }

    /// <summary>
    /// Estimated time remaining (null if unknown or charging).
    /// </summary>
    public TimeSpan? TimeRemaining { get; init; }

    /// <summary>
    /// Whether the system is on battery power.
    /// </summary>
    public bool IsOnBattery => Status == BatteryStatus.Discharging;

    /// <summary>
    /// Whether battery is charging.
    /// </summary>
    public bool IsCharging => Status == BatteryStatus.Charging;

    /// <summary>
    /// Whether battery is full.
    /// </summary>
    public bool IsFull => Status == BatteryStatus.Full;
}

/// <summary>
/// Battery status enumeration.
/// </summary>
public enum BatteryStatus
{
    Unknown,
    Charging,
    Discharging,
    Full,
    NotPresent
}

/// <summary>
/// Network information.
/// </summary>
public sealed record NetworkInfo
{
    /// <summary>
    /// Whether network is connected.
    /// </summary>
    public bool IsConnected { get; init; }

    /// <summary>
    /// Connection type.
    /// </summary>
    public NetworkConnectionType ConnectionType { get; init; }

    /// <summary>
    /// Network name (SSID for WiFi).
    /// </summary>
    public string? NetworkName { get; init; }

    /// <summary>
    /// Signal strength (0-100 for WiFi, null for ethernet).
    /// </summary>
    public int? SignalStrength { get; init; }

    /// <summary>
    /// Whether connection is metered.
    /// </summary>
    public bool IsMetered { get; init; }

    /// <summary>
    /// Whether internet access is available.
    /// </summary>
    public bool HasInternetAccess { get; init; }
}

/// <summary>
/// Network connection type.
/// </summary>
public enum NetworkConnectionType
{
    Unknown,
    Ethernet,
    Wifi,
    Cellular,
    VPN
}