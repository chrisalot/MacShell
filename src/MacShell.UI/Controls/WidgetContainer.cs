using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MacShell.Core.Interfaces;

namespace MacShell.UI.Controls;

/// <summary>
/// Container for hosting widget content.
/// </summary>
public sealed class WidgetContainer : ContentControl
{
    public WidgetContainer()
    {
        DefaultStyleKey = typeof(WidgetContainer);
    }

    #region Widget Property

    public IWidget? Widget
    {
        get => (IWidget?)GetValue(WidgetProperty);
        set => SetValue(WidgetProperty, value);
    }

    public static readonly DependencyProperty WidgetProperty =
        DependencyProperty.Register(nameof(Widget), typeof(IWidget), typeof(WidgetContainer),
            new PropertyMetadata(null, OnWidgetChanged));

    private static void OnWidgetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WidgetContainer container)
        {
            container.UpdateContent();
        }
    }

    #endregion

    private void UpdateContent()
    {
        if (Widget != null)
        {
            Content = Widget.GetView();
        }
        else
        {
            Content = null;
        }
    }
}