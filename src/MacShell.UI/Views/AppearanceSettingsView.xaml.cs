using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MacShell.UI.Views;

/// <summary>
/// View model for Appearance settings.
/// </summary>
internal sealed partial class AppearanceSettingsViewModel : ObservableObject
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
public sealed partial class AppearanceSettingsView : Page
{
    public AppearanceSettingsViewModel ViewModel { get; } = new();

    public AppearanceSettingsView()
    {
        InitializeComponent();
        DataContext = ViewModel;
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
            var root = (App)App.Current;
            root.RequestedTheme = theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };
        }
    }

    private void OnAccentColorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AccentColorCombo.SelectedItem is ComboBoxItem item && item.Tag is string colorHex)
        {
            if (ColorHelper.TryParseColor(colorHex, out var color))
            {
                Resources["MacShellAccentColor"] = color;
                Resources["MacShellAccentBrush"] = new SolidColorBrush(color);
                AccentColorPreview.Background = new SolidColorBrush(color);
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
}

internal static class ColorHelper
{
    public static bool TryParseColor(string hex, out Color color)
    {
        color = Colors.Transparent;
        try
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                var r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                var g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                var b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                color = Color.FromArgb(255, r, g, b);
                return true;
            }
        }
        catch { }
        return false;
    }
}