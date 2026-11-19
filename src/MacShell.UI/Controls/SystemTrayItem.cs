using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MacShell.UI.Controls;

/// <summary>
/// A system tray item with icon and optional text.
/// </summary>
public sealed class SystemTrayItem : Control
{
    public SystemTrayItem()
    {
        DefaultStyleKey = typeof(SystemTrayItem);
    }

    #region Icon Property

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(SystemTrayItem),
            new PropertyMetadata(""));

    #endregion

    #region Text Property

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(SystemTrayItem),
            new PropertyMetadata(""));

    #endregion

    #region IconFontFamily Property

    public FontFamily IconFontFamily
    {
        get => (FontFamily)GetValue(IconFontFamilyProperty);
        set => SetValue(IconFontFamilyProperty, value);
    }

    public static readonly DependencyProperty IconFontFamilyProperty =
        DependencyProperty.Register(nameof(IconFontFamily), typeof(FontFamily), typeof(SystemTrayItem),
            new PropertyMetadata(new FontFamily("Segoe Fluent Icons")));

    #endregion

    #region IconSize Property

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(SystemTrayItem),
            new PropertyMetadata(14.0));

    #endregion

    #region Spacing Property

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public static readonly DependencyProperty SpacingProperty =
        DependencyProperty.Register(nameof(Spacing), typeof(double), typeof(SystemTrayItem),
            new PropertyMetadata(6.0));

    #endregion
}