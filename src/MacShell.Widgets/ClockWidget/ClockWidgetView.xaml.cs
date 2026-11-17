using Microsoft.UI.Xaml.Controls;
using MacShell.Widgets.ClockWidget;

namespace MacShell.Widgets.ClockWidget;

/// <summary>
/// Clock widget view.
/// </summary>
public sealed partial class ClockWidgetView : UserControl
{
    private readonly ClockWidget _widget;

    public ClockWidgetView(ClockWidget widget)
    {
        _widget = widget;
        InitializeComponent();
    }

    public void UpdateTime(ClockSettings settings)
    {
        var now = DateTime.Now;

        // Format time
        string timeFormat;
        if (settings.Format == "12h")
        {
            timeFormat = settings.ShowSeconds ? "h:mm:ss tt" : "h:mm tt";
        }
        else
        {
            timeFormat = settings.ShowSeconds ? "HH:mm:ss" : "HH:mm";
        }
        TimeText.Text = now.ToString(timeFormat);

        // Format date
        if (settings.ShowDate)
        {
            string dateFormat = settings.DateFormat switch
            {
                "long" => "dddd, MMMM d, yyyy",
                "iso" => "yyyy-MM-dd",
                _ => "M/d/yyyy"
            };
            DateText.Text = now.ToString(dateFormat);
            DateText.Visibility = Visibility.Visible;
        }
        else
        {
            DateText.Visibility = Visibility.Collapsed;
        }
    }
}