using System;
using System.IO;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MacShell.UI.Views;

/// <summary>
/// View model for Dock settings.
/// </summary>
public sealed partial class DockSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _iconSize = 48;

    [ObservableProperty]
    private bool _enableMagnification = true;

    [ObservableProperty]
    private double _magnificationFactor = 1.4;

    [ObservableProperty]
    private bool _enableAnimations = true;

    [ObservableProperty]
    private bool _showRunningIndicators = true;

    [ObservableProperty]
    private bool _autoHide = false;

    [ObservableProperty]
    private int _autoHideDelay = 200;
}

/// <summary>
/// Dock settings page.
/// </summary>
public sealed class DockSettingsView : Page
{
    public DockSettingsViewModel ViewModel { get; } = new();

    private RadioButton? _posBottom;
    private RadioButton? _posLeft;
    private RadioButton? _posRight;
    private Slider? _iconSizeSlider;
    private TextBlock? _iconSizeText;
    private ToggleSwitch? _magnificationToggle;
    private StackPanel? _magnificationPanel;
    private Slider? _magnificationFactorSlider;
    private TextBlock? _magnificationFactorText;
    private ToggleSwitch? _animationsToggle;
    private ToggleSwitch? _runningIndicatorsToggle;
    private ToggleSwitch? _autoHideToggle;
    private StackPanel? _autoHidePanel;
    private Slider? _autoHideDelaySlider;
    private TextBlock? _autoHideDelayText;
    private Button? _managePinnedAppsButton;

    public DockSettingsView()
    {
        LoadXaml();
        SetupEventHandlers();
        DataContext = ViewModel;
    }

    private void LoadXaml()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? "";
        var xamlPath = Path.Combine(assemblyDir, "Views", "DockSettingsView.xaml");

        if (!File.Exists(xamlPath))
        {
            throw new FileNotFoundException($"DockSettingsView.xaml not found at {xamlPath}");
        }

        var xamlContent = File.ReadAllText(xamlPath);
        var page = (Page)XamlReader.Load(xamlPath);

        // Copy properties
        this.Background = page.Background;
        this.Content = page.Content;

        // Find named elements
        _posBottom = FindName("PosBottom") as RadioButton;
        _posLeft = FindName("PosLeft") as RadioButton;
        _posRight = FindName("PosRight") as RadioButton;
        _iconSizeSlider = FindName("IconSizeSlider") as Slider;
        _iconSizeText = FindName("IconSizeText") as TextBlock;
        _magnificationToggle = FindName("MagnificationToggle") as ToggleSwitch;
        _magnificationPanel = FindName("MagnificationPanel") as StackPanel;
        _magnificationFactorSlider = FindName("MagnificationFactorSlider") as Slider;
        _magnificationFactorText = FindName("MagnificationFactorText") as TextBlock;
        _animationsToggle = FindName("AnimationsToggle") as ToggleSwitch;
        _runningIndicatorsToggle = FindName("RunningIndicatorsToggle") as ToggleSwitch;
        _autoHideToggle = FindName("AutoHideToggle") as ToggleSwitch;
        _autoHidePanel = FindName("AutoHidePanel") as StackPanel;
        _autoHideDelaySlider = FindName("AutoHideDelaySlider") as Slider;
        _autoHideDelayText = FindName("AutoHideDelayText") as TextBlock;
        _managePinnedAppsButton = FindName("ManagePinnedAppsButton") as Button;
    }

    private void SetupEventHandlers()
    {
        if (_iconSizeSlider != null)
        {
            _iconSizeSlider.ValueChanged += (s, e) =>
            {
                if (_iconSizeText != null)
                    _iconSizeText.Text = $"{(int)_iconSizeSlider.Value}";
            };
        }

        if (_magnificationToggle != null && _magnificationPanel != null)
        {
            _magnificationToggle.Toggled += (s, e) =>
            {
                _magnificationPanel.Visibility = _magnificationToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
            };
        }

        if (_magnificationFactorSlider != null && _magnificationFactorText != null)
        {
            _magnificationFactorSlider.ValueChanged += (s, e) =>
            {
                _magnificationFactorText.Text = $"{_magnificationFactorSlider.Value:F1}";
            };
        }

        if (_autoHideToggle != null && _autoHidePanel != null)
        {
            _autoHideToggle.Toggled += (s, e) =>
            {
                _autoHidePanel.Visibility = _autoHideToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
            };
        }

        if (_autoHideDelaySlider != null && _autoHideDelayText != null)
        {
            _autoHideDelaySlider.ValueChanged += (s, e) =>
            {
                _autoHideDelayText.Text = $"{(int)_autoHideDelaySlider.Value} ms";
            };
        }

        if (_managePinnedAppsButton != null)
        {
            _managePinnedAppsButton.Click += OnManagePinnedApps;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is SettingsContext context)
        {
            // Load settings from context.DockManager
        }
    }

    private void OnManagePinnedApps(object sender, RoutedEventArgs e)
    {
        // TODO: Open pinned apps management dialog
    }
}