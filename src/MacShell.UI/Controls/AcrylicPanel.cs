using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace MacShell.UI.Controls;

/// <summary>
/// A panel with acrylic background that can be toggled for performance.
/// </summary>
public sealed class AcrylicPanel : Panel
{
    private AcrylicBrush? _acrylicBrush;
    private bool _isAcrylicEnabled = true;

    public AcrylicPanel()
    {
        Loaded += OnLoaded;
    }

    #region AcrylicBrush Property

    public AcrylicBrush? AcrylicBrush
    {
        get => (AcrylicBrush?)GetValue(AcrylicBrushProperty);
        set => SetValue(AcrylicBrushProperty, value);
    }

    public static readonly DependencyProperty AcrylicBrushProperty =
        DependencyProperty.Register(nameof(AcrylicBrush), typeof(AcrylicBrush), typeof(AcrylicPanel),
            new PropertyMetadata(null, OnAcrylicBrushChanged));

    private static void OnAcrylicBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AcrylicPanel panel)
        {
            panel.UpdateBackground();
        }
    }

    #endregion

    #region IsAcrylicEnabled Property

    public bool IsAcrylicEnabled
    {
        get => (bool)GetValue(IsAcrylicEnabledProperty);
        set => SetValue(IsAcrylicEnabledProperty, value);
    }

    public static readonly DependencyProperty IsAcrylicEnabledProperty =
        DependencyProperty.Register(nameof(IsAcrylicEnabled), typeof(bool), typeof(AcrylicPanel),
            new PropertyMetadata(true, OnIsAcrylicEnabledChanged));

    private static void OnIsAcrylicEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AcrylicPanel panel)
        {
            panel._isAcrylicEnabled = (bool)e.NewValue;
            panel.UpdateBackground();
        }
    }

    #endregion

    #region FallbackBrush Property

    public Brush? FallbackBrush
    {
        get => (Brush?)GetValue(FallbackBrushProperty);
        set => SetValue(FallbackBrushProperty, value);
    }

    public static readonly DependencyProperty FallbackBrushProperty =
        DependencyProperty.Register(nameof(FallbackBrush), typeof(Brush), typeof(AcrylicPanel),
            new PropertyMetadata(null, OnFallbackBrushChanged));

    private static void OnFallbackBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AcrylicPanel panel)
        {
            panel.UpdateBackground();
        }
    }

    #endregion

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateBackground();
    }

    private void UpdateBackground()
    {
        if (!_isAcrylicEnabled || AcrylicBrush == null)
        {
            Background = FallbackBrush ?? new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            return;
        }

        // Check if acrylic is supported (Windows 10 1809+)
        if (IsAcrylicSupported())
        {
            Background = AcrylicBrush;
        }
        else
        {
            Background = FallbackBrush ?? new SolidColorBrush(AcrylicBrush.FallbackColor);
        }
    }

    private static bool IsAcrylicSupported()
    {
        // Acrylic requires Windows 10 1809 (build 17763) or later
        var version = Environment.OSVersion.Version;
        return version.Major > 10 || (version.Major == 10 && version.Build >= 17763);
    }
}