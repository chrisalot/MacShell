using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MacShell.Core.Interfaces;

namespace MacShell.UI.Controls;

/// <summary>
/// Top menu bar with system status items (time, battery, network).
/// </summary>
public sealed class TopBar : Control
{
    private TextBlock? _timeText;
    private SystemTrayItem? _batteryItem;
    private SystemTrayItem? _networkItem;
    private DispatcherTimer? _timeTimer;
    private ISystemService? _systemService;

    public TopBar()
    {
        DefaultStyleKey = typeof(TopBar);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #region SystemService Property

    public ISystemService? SystemService
    {
        get => (ISystemService?)GetValue(SystemServiceProperty);
        set => SetValue(SystemServiceProperty, value);
    }

    public static readonly DependencyProperty SystemServiceProperty =
        DependencyProperty.Register(nameof(SystemService), typeof(ISystemService), typeof(TopBar),
            new PropertyMetadata(null, OnSystemServiceChanged));

    private static void OnSystemServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TopBar bar)
        {
            bar._systemService = e.NewValue as ISystemService;
            bar.BindSystemService();
        }
    }

    #endregion

    #region TimeFormat Property

    public string TimeFormat
    {
        get => (string)GetValue(TimeFormatProperty);
        set => SetValue(TimeFormatProperty, value);
    }

    public static readonly DependencyProperty TimeFormatProperty =
        DependencyProperty.Register(nameof(TimeFormat), typeof(string), typeof(TopBar),
            new PropertyMetadata("HH:mm", OnTimeFormatChanged));

    private static void OnTimeFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TopBar bar)
        {
            bar.UpdateTime();
        }
    }

    #endregion

    #region ShowSeconds Property

    public bool ShowSeconds
    {
        get => (bool)GetValue(ShowSecondsProperty);
        set => SetValue(ShowSecondsProperty, value);
    }

    public static readonly DependencyProperty ShowSecondsProperty =
        DependencyProperty.Register(nameof(ShowSeconds), typeof(bool), typeof(TopBar),
            new PropertyMetadata(false, OnShowSecondsChanged));

    private static void OnShowSecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TopBar bar)
        {
            bar.UpdateTime();
        }
    }

    #endregion

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _timeText = GetTemplateChild("TimeText") as TextBlock;
        _batteryItem = GetTemplateChild("BatteryItem") as SystemTrayItem;
        _networkItem = GetTemplateChild("NetworkItem") as SystemTrayItem;

        UpdateTime();
        BindSystemService();
        StartTimeTimer();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        StartTimeTimer();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopTimeTimer();
    }

    private void StartTimeTimer()
    {
        StopTimeTimer();
        _timeTimer = new DispatcherTimer
        {
            Interval = ShowSeconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(30)
        };
        _timeTimer.Tick += (s, e) => UpdateTime();
        _timeTimer.Start();
    }

    private void StopTimeTimer()
    {
        _timeTimer?.Stop();
        _timeTimer = null;
    }

    private void UpdateTime()
    {
        if (_timeText != null)
        {
            var format = ShowSeconds ? "HH:mm:ss" : TimeFormat;
            _timeText.Text = DateTime.Now.ToString(format);
        }
    }

    private void BindSystemService()
    {
        if (_systemService == null) return;

        // Battery
        if (_batteryItem != null)
        {
            _systemService.BatteryChanged -= OnBatteryChanged;
            _systemService.BatteryChanged += OnBatteryChanged;
            UpdateBattery(_systemService.Battery);
        }

        // Network
        if (_networkItem != null)
        {
            _systemService.NetworkChanged -= OnNetworkChanged;
            _systemService.NetworkChanged += OnNetworkChanged;
            UpdateNetwork(_systemService.Network);
        }
    }

    private void OnBatteryChanged(object? sender, MacShell.Core.Interfaces.BatteryInfo battery)
    {
        UpdateBattery(battery);
    }

    private void OnNetworkChanged(object? sender, MacShell.Core.Interfaces.NetworkInfo network)
    {
        UpdateNetwork(network);
    }

    private void UpdateBattery(MacShell.Core.Interfaces.BatteryInfo battery)
    {
        if (_batteryItem == null) return;

        if (battery.Percentage.HasValue)
        {
            _batteryItem.Text = $"{battery.Percentage}%";
            _batteryItem.Icon = battery.Status switch
            {
                MacShell.Core.Interfaces.BatteryStatus.Charging => "BatteryCharging",
                MacShell.Core.Interfaces.BatteryStatus.Full => "BatteryFull",
                MacShell.Core.Interfaces.BatteryStatus.Discharging => "BatteryDischarging",
                _ => "BatteryUnknown"
            };
            _batteryItem.Visibility = Visibility.Visible;
        }
        else
        {
            _batteryItem.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateNetwork(MacShell.Core.Interfaces.NetworkInfo network)
    {
        if (_networkItem == null) return;

        if (network.IsConnected)
        {
            _networkItem.Icon = network.ConnectionType switch
            {
                MacShell.Core.Interfaces.NetworkConnectionType.Wifi => "Wifi",
                MacShell.Core.Interfaces.NetworkConnectionType.Ethernet => "Ethernet",
                MacShell.Core.Interfaces.NetworkConnectionType.Cellular => "Cellular",
                MacShell.Core.Interfaces.NetworkConnectionType.VPN => "VPN",
                _ => "NetworkConnected"
            };

            if (network.ConnectionType == MacShell.Core.Interfaces.NetworkConnectionType.Wifi &&
                network.SignalStrength.HasValue)
            {
                _networkItem.Text = $"{network.SignalStrength}%";
            }
            else
            {
                _networkItem.Text = network.NetworkName ?? "";
            }
            _networkItem.Visibility = Visibility.Visible;
        }
        else
        {
            _networkItem.Icon = "NetworkDisconnected";
            _networkItem.Text = "No Connection";
            _networkItem.Visibility = Visibility.Visible;
        }
    }
}