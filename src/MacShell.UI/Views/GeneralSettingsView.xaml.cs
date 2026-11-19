using System;
using System.IO;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MacShell.UI.Views;

/// <summary>
/// View model for General settings.
/// </summary>
public sealed partial class GeneralSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _startAtLogin = true;

    [ObservableProperty]
    private bool _restorePosition = true;

    [ObservableProperty]
    private bool _checkForUpdates = true;

    [ObservableProperty]
    private bool _enableDebugLogging = false;

    [ObservableProperty]
    private bool _highPerformanceMode = false;

    [RelayCommand]
    public void ChangeLauncherHotkey()
    {
        // TODO: Implement hotkey change dialog
    }

    [RelayCommand]
    public void ChangeSettingsHotkey()
    {
        // TODO: Implement hotkey change dialog
    }
}

/// <summary>
/// General settings page.
/// </summary>
public sealed class GeneralSettingsView : Page
{
    public GeneralSettingsViewModel ViewModel { get; } = new();

    private ToggleSwitch? _startAtLoginToggle;
    private ToggleSwitch? _restorePositionToggle;
    private ToggleSwitch? _checkForUpdatesToggle;
    private ToggleSwitch? _debugLoggingToggle;
    private ToggleSwitch? _highPerformanceToggle;
    private Button? _changeLauncherHotkeyButton;
    private Button? _changeSettingsHotkeyButton;

    public GeneralSettingsView()
    {
        LoadXaml();
        SetupEventHandlers();
        DataContext = ViewModel;
    }

    private void LoadXaml()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? "";
        var xamlPath = Path.Combine(assemblyDir, "Views", "GeneralSettingsView.xaml");

        if (!File.Exists(xamlPath))
        {
            throw new FileNotFoundException($"GeneralSettingsView.xaml not found at {xamlPath}");
        }

        var xamlContent = File.ReadAllText(xamlPath);
        var page = (Page)XamlReader.Load(xamlContent);

        // Copy properties
        this.Background = page.Background;
        this.Content = page.Content;

        // Find named elements
        _startAtLoginToggle = FindName("StartAtLoginToggle") as ToggleSwitch;
        _restorePositionToggle = FindName("RestorePositionToggle") as ToggleSwitch;
        _checkForUpdatesToggle = FindName("CheckForUpdatesToggle") as ToggleSwitch;
        _debugLoggingToggle = FindName("DebugLoggingToggle") as ToggleSwitch;
        _highPerformanceToggle = FindName("HighPerformanceToggle") as ToggleSwitch;
        _changeLauncherHotkeyButton = FindName("ChangeLauncherHotkeyButton") as Button;
        _changeSettingsHotkeyButton = FindName("ChangeSettingsHotkeyButton") as Button;

        // Bind toggles to ViewModel
        if (_startAtLoginToggle != null)
            _startAtLoginToggle.IsOn = ViewModel.StartAtLogin;
        if (_restorePositionToggle != null)
            _restorePositionToggle.IsOn = ViewModel.RestorePosition;
        if (_checkForUpdatesToggle != null)
            _checkForUpdatesToggle.IsOn = ViewModel.CheckForUpdates;
        if (_debugLoggingToggle != null)
            _debugLoggingToggle.IsOn = ViewModel.EnableDebugLogging;
        if (_highPerformanceToggle != null)
            _highPerformanceToggle.IsOn = ViewModel.HighPerformanceMode;
    }

    private void SetupEventHandlers()
    {
        if (_startAtLoginToggle != null)
            _startAtLoginToggle.Toggled += (s, e) => ViewModel.StartAtLogin = _startAtLoginToggle.IsOn;
        if (_restorePositionToggle != null)
            _restorePositionToggle.Toggled += (s, e) => ViewModel.RestorePosition = _restorePositionToggle.IsOn;
        if (_checkForUpdatesToggle != null)
            _checkForUpdatesToggle.Toggled += (s, e) => ViewModel.CheckForUpdates = _checkForUpdatesToggle.IsOn;
        if (_debugLoggingToggle != null)
            _debugLoggingToggle.Toggled += (s, e) => ViewModel.EnableDebugLogging = _debugLoggingToggle.IsOn;
        if (_highPerformanceToggle != null)
            _highPerformanceToggle.Toggled += (s, e) => ViewModel.HighPerformanceMode = _highPerformanceToggle.IsOn;
        if (_changeLauncherHotkeyButton != null)
            _changeLauncherHotkeyButton.Click += (s, e) => ViewModel.ChangeLauncherHotkey();
        if (_changeSettingsHotkeyButton != null)
            _changeSettingsHotkeyButton.Click += (s, e) => ViewModel.ChangeSettingsHotkey();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is SettingsContext context)
        {
            // Load settings from context
        }
    }
}