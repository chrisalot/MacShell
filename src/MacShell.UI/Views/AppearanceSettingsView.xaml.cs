using System;
using System.IO;
using System.Reflection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.UI;

namespace MacShell.UI.Views;

/// <summary>
/// View model for Appearance settings.
/// </summary>
public sealed partial class AppearanceSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _enableTransparency = true;

    [ObservableProperty]
    private int _transparencyLevel = 60;

    [ObservableProperty]
    private bool _enableShadows = true;

    [ObservableProperty]
    private bool _followSystemScaling = true;
}

/// <summary>
/// Appearance settings page.
/// </summary>
public sealed class AppearanceSettingsView : Page
{
    public AppearanceSettingsViewModel ViewModel { get; } = new();

    private RadioButton? _themeDark;
    private RadioButton? _themeLight;
    private RadioButton? _themeSystem;
    private ComboBox? _accentColorCombo;
    private Border? _accentColorPreview;
    private ToggleSwitch? _transparencyToggle;
    private StackPanel? _transparencyLevelPanel;
    private TextBlock? _transparencyLevelText;
    private Slider? _transparencySlider;
    private RadioButton? _cornerSmall;
    private RadioButton? _cornerMedium;
    private RadioButton? _cornerLarge;
    private RadioButton? _cornerRound;
    private ToggleSwitch? _shadowsToggle;
    private ToggleSwitch? _scalingToggle;

    public AppearanceSettingsView()
    {
        LoadXaml();
        SetupEventHandlers();
        DataContext = ViewModel;
    }

    private void LoadXaml()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? "";
        var xamlPath = Path.Combine(assemblyDir, "Views", "AppearanceSettingsView.xaml");

        if (!File.Exists(xamlPath))
        {
            throw new FileNotFoundException($"AppearanceSettingsView.xaml not found at {xamlPath}");
        }

        var xamlContent = File.ReadAllText(xamlPath);
        var page = (Page)XamlReader.Load(xamlContent);

        // Copy properties
        this.Background = page.Background;
        this.Content = page.Content;

        // Find named elements
        _themeDark = FindName("ThemeDark") as RadioButton;
        _themeLight = FindName("ThemeLight") as RadioButton;
        _themeSystem = FindName("ThemeSystem") as RadioButton;
        _accentColorCombo = FindName("AccentColorCombo") as ComboBox;
        _accentColorPreview = FindName("AccentColorPreview") as Border;
        _transparencyToggle = FindName("TransparencyToggle") as ToggleSwitch;
        _transparencyLevelPanel = FindName("TransparencyLevelPanel") as StackPanel;
        _transparencyLevelText = FindName("TransparencyLevelText") as TextBlock;
        _transparencySlider = FindName("TransparencySlider") as Slider;
        _cornerSmall = FindName("CornerSmall") as RadioButton;
        _cornerMedium = FindName("CornerMedium") as RadioButton;
        _cornerLarge = FindName("CornerLarge") as RadioButton;
        _cornerRound = FindName("CornerRound") as RadioButton;
        _shadowsToggle = FindName("ShadowsToggle") as ToggleSwitch;
        _scalingToggle = FindName("ScalingToggle") as ToggleSwitch;
    }

    private void SetupEventHandlers()
    {
        if (_themeDark != null) _themeDark.Checked += OnThemeChanged;
        if (_themeLight != null) _themeLight.Checked += OnThemeChanged;
        if (_themeSystem != null) _themeSystem.Checked += OnThemeChanged;
        if (_accentColorCombo != null) _accentColorCombo.SelectionChanged += OnAccentColorChanged;
        if (_transparencyToggle != null) _transparencyToggle.Toggled += OnTransparencyToggled;
        if (_transparencySlider != null) _transparencySlider.ValueChanged += OnTransparencyLevelChanged;
        if (_cornerSmall != null) _cornerSmall.Checked += OnCornerRadiusChanged;
        if (_cornerMedium != null) _cornerMedium.Checked += OnCornerRadiusChanged;
        if (_cornerLarge != null) _cornerLarge.Checked += OnCornerRadiusChanged;
        if (_cornerRound != null) _cornerRound.Checked += OnCornerRadiusChanged;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is SettingsContext context)
        {
            // Load settings
        }
    }

    private void OnThemeChanged(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string theme)
        {
            var root = Microsoft.UI.Xaml.Application.Current;
            if (root != null)
            {
                root.RequestedTheme = theme switch
                {
                    "Light" => ApplicationTheme.Light,
                    "Dark" => ApplicationTheme.Dark,
                    _ => ApplicationTheme.Light // Default to Light, no Default option in ApplicationTheme
                };
            }
        }
    }

    private void OnAccentColorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_accentColorCombo?.SelectedItem is ComboBoxItem item && item.Tag is string colorHex)
        {
            if (ColorHelper.TryParseColor(colorHex, out var color))
            {
                Resources["MacShellAccentColor"] = color;
                Resources["MacShellAccentBrush"] = new SolidColorBrush(color);
                if (_accentColorPreview != null)
                    _accentColorPreview.Background = new SolidColorBrush(color);
            }
        }
    }

    private void OnCornerRadiusChanged(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string radiusStr && int.TryParse(radiusStr, out var radius))
        {
            Resources["MacShellCornerRadiusMedium"] = (double)radius;
            Resources["MacShellCornerRadiusLarge"] = (double)radius + 4;
            Resources["MacShellCornerRadiusXLarge"] = (double)radius + 8;
        }
    }

    private void OnTransparencyToggled(object sender, RoutedEventArgs e)
    {
        if (_transparencyToggle != null && _transparencyLevelPanel != null)
        {
            _transparencyLevelPanel.Visibility = _transparencyToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnTransparencyLevelChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_transparencyLevelText != null && _transparencySlider != null)
        {
            _transparencyLevelText.Text = $"{(int)_transparencySlider.Value}%";
        }
    }
}

internal static class ColorHelper
{
    public static bool TryParseColor(string hex, out Windows.UI.Color color)
    {
        color = Microsoft.UI.Colors.Transparent;
        try
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                var r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                var g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                var b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                color = Windows.UI.Color.FromArgb(255, r, g, b);
                return true;
            }
        }
        catch { }
        return false;
    }
}