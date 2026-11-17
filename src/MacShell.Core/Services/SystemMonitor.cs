using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;
using MacShell.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MacShell.Core.Services;

/// <summary>
/// Event-driven system monitor for battery, network, and time.
/// Zero CPU when idle - uses system events only.
/// </summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public sealed class SystemMonitor : ISystemService, IDisposable
{
    private readonly ILogger<SystemMonitor> _logger;
    private readonly System.Threading.Timer _timeTimer;
    private BatteryInfo _battery;
    private NetworkInfo _network;
    private bool _disposed;

    public SystemMonitor(ILogger<SystemMonitor> logger)
    {
        _logger = logger;
        _battery = new BatteryInfo { Percentage = null, Status = BatteryStatus.Unknown, TimeRemaining = null };
        _network = new NetworkInfo { IsConnected = false, ConnectionType = NetworkConnectionType.Unknown };

        // Time timer - fires every second
        _timeTimer = new System.Threading.Timer(OnTimeTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        // Subscribe to system events
        Microsoft.Win32.SystemEvents.PowerModeChanged += OnPowerModeChanged;
        Microsoft.Win32.SystemEvents.SessionSwitch += OnSessionSwitch;

        // Initial readings
        UpdateBattery();
        UpdateNetwork();

        // Network change event (Windows 10+)
        try
        {
            Windows.Networking.Connectivity.NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
        }
        catch
        {
            // NetworkInformation not available
        }
    }

    public BatteryInfo Battery => _battery;
    public NetworkInfo Network => _network;
    public DateTime CurrentTime => DateTime.Now;
    public TimeZoneInfo TimeZone => TimeZoneInfo.Local;

    public event EventHandler<BatteryInfo>? BatteryChanged;
    public event EventHandler<NetworkInfo>? NetworkChanged;
    public event EventHandler<DateTime>? TimeChanged;

    public void StartMonitoring()
    {
        // Already monitoring via events
        _logger.LogDebug("System monitoring started");
    }

    public void StopMonitoring()
    {
        // Events are automatic, but we can stop the time timer if needed
        _logger.LogDebug("System monitoring stopped");
    }

    private void OnTimeTick(object? state)
    {
        TimeChanged?.Invoke(this, DateTime.Now);
    }

    private void OnPowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
    {
        _logger.LogDebug("Power mode changed: {Mode}", e.Mode);
        UpdateBattery();
    }

    private void OnSessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
    {
        if (e.Reason == Microsoft.Win32.SessionSwitchReason.SessionUnlock ||
            e.Reason == Microsoft.Win32.SessionSwitchReason.SessionLogon)
        {
            UpdateBattery();
            UpdateNetwork();
        }
    }

    private void OnNetworkStatusChanged(object sender)
    {
        UpdateNetwork();
    }

    private void UpdateBattery()
    {
        try
        {
            var newBattery = GetBatteryInfo();
            if (!BatteryEquals(_battery, newBattery))
            {
                _battery = newBattery;
                BatteryChanged?.Invoke(this, _battery);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update battery info");
        }
    }

    private void UpdateNetwork()
    {
        try
        {
            var newNetwork = GetNetworkInfo();
            if (!NetworkEquals(_network, newNetwork))
            {
                _network = newNetwork;
                NetworkChanged?.Invoke(this, _network);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update network info");
        }
    }

    private BatteryInfo GetBatteryInfo()
    {
        // Use System.Diagnostics.Process to query WMI or use Windows APIs
        // For simplicity, using SystemInformation (Windows Forms) approach
        try
        {
            var powerStatus = SystemInformation.PowerStatus;
            var percentage = (int)(powerStatus.BatteryLifePercent * 100);
            var status = powerStatus.PowerLineStatus switch
            {
                PowerLineStatus.Online => BatteryStatus.Charging,
                PowerLineStatus.Offline => BatteryStatus.Discharging,
                PowerLineStatus.Unknown => BatteryStatus.Unknown,
                _ => BatteryStatus.Unknown
            };

            // BatteryLifeRemaining returns seconds, -1 if unknown
            TimeSpan? timeRemaining = null;
            if (powerStatus.BatteryLifeRemaining > 0 && status == BatteryStatus.Discharging)
            {
                timeRemaining = TimeSpan.FromSeconds(powerStatus.BatteryLifeRemaining);
            }

            return new BatteryInfo
            {
                Percentage = percentage >= 0 && percentage <= 100 ? percentage : null,
                Status = status,
                TimeRemaining = timeRemaining
            };
        }
        catch
        {
            return new BatteryInfo { Percentage = null, Status = BatteryStatus.NotPresent, TimeRemaining = null };
        }
    }

    private NetworkInfo GetNetworkInfo()
    {
        try
        {
            var profiles = Windows.Networking.Connectivity.NetworkInformation.GetConnectionProfiles();
            var internetProfile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();

            if (internetProfile == null)
            {
                return new NetworkInfo
                {
                    IsConnected = false,
                    ConnectionType = NetworkConnectionType.Unknown,
                    HasInternetAccess = false
                };
            }

            var isConnected = internetProfile.GetNetworkConnectivityLevel() == Windows.Networking.Connectivity.NetworkConnectivityLevel.InternetAccess;
            var connectionType = GetConnectionType(internetProfile);
            var signalStrength = GetSignalStrength(internetProfile);

            var connectionCost = internetProfile.GetConnectionCost();
            var isMetered = connectionCost?.OverDataLimit == true || connectionCost?.ApproachingDataLimit == true;

            return new NetworkInfo
            {
                IsConnected = isConnected,
                ConnectionType = connectionType,
                NetworkName = internetProfile.ProfileName,
                SignalStrength = signalStrength,
                IsMetered = isMetered,
                HasInternetAccess = isConnected
            };
        }
        catch
        {
            return new NetworkInfo
            {
                IsConnected = false,
                ConnectionType = NetworkConnectionType.Unknown,
                HasInternetAccess = false
            };
        }
    }

    private static NetworkConnectionType GetConnectionType(Windows.Networking.Connectivity.ConnectionProfile profile)
    {
        if (profile.IsWlanConnectionProfile)
            return NetworkConnectionType.Wifi;
        if (profile.IsWwanConnectionProfile)
            return NetworkConnectionType.Cellular;
        if (profile.NetworkAdapter != null)
        {
            var adapter = profile.NetworkAdapter;
            if (adapter.IanaInterfaceType == 6) // IF_TYPE_ETHERNET_CSMACD
                return NetworkConnectionType.Ethernet;
        }
        return NetworkConnectionType.Unknown;
    }

    private static int? GetSignalStrength(Windows.Networking.Connectivity.ConnectionProfile profile)
    {
        if (profile.IsWlanConnectionProfile)
        {
            try
            {
                var signalBars = profile.GetSignalBars();
                return (int)(signalBars * 25); // Convert 0-4 bars to 0-100
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    private static bool BatteryEquals(BatteryInfo a, BatteryInfo b)
    {
        return a.Percentage == b.Percentage && a.Status == b.Status && a.TimeRemaining == b.TimeRemaining;
    }

    private static bool NetworkEquals(NetworkInfo a, NetworkInfo b)
    {
        return a.IsConnected == b.IsConnected &&
               a.ConnectionType == b.ConnectionType &&
               a.NetworkName == b.NetworkName &&
               a.SignalStrength == b.SignalStrength &&
               a.IsMetered == b.IsMetered &&
               a.HasInternetAccess == b.HasInternetAccess;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _timeTimer.Dispose();
        Microsoft.Win32.SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        Microsoft.Win32.SystemEvents.SessionSwitch -= OnSessionSwitch;

        try
        {
            Windows.Networking.Connectivity.NetworkInformation.NetworkStatusChanged -= OnNetworkStatusChanged;
        }
        catch
        {
            // Ignore
        }
    }
}