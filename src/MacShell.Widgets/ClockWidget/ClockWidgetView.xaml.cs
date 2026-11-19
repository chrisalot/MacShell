using System;
using System.IO;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using MacShell.Widgets.ClockWidget;

namespace MacShell.Widgets.ClockWidget;

/// <summary>
/// Clock widget view - uses runtime XAML loading via XamlReader.Load().
/// </summary>
public sealed class ClockWidgetView : UserControl
{
    private readonly ClockWidget _widget;

    // Named XAML elements
    private TextBlock? _timeText;
    private TextBlock? _dateText;

    public ClockWidgetView(ClockWidget widget)
    {
        _widget = widget;
        LoadXaml();
    }

    private void LoadXaml()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? "";
        var xamlPath = Path.Combine(assemblyDir, "ClockWidgetView.xaml");

        if (!File.Exists(xamlPath))
        {
            throw new FileNotFoundException($"ClockWidgetView.xaml not found at {xamlPath}");
        }

        var xamlContent = File.ReadAllText(xamlPath);
        var control = (UserControl)XamlReader.Load(xamlContent);

        // Copy content from loaded control to this instance
        this.Content = control.Content;

        // Find named elements from the loaded content
        if (this.Content is FrameworkElement root)
        {
            _timeText = root.FindName("TimeText") as TextBlock;
            _dateText = root.FindName("DateText") as TextBlock;
        }
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
        if (_timeText != null) _timeText.Text = now.ToString(timeFormat);

        // Format date
        if (_dateText != null)
        {
            if (settings.ShowDate)
            {
                string dateFormat = settings.DateFormat switch
                {
                    "long" => "dddd, MMMM d, yyyy",
                    "iso" => "yyyy-MM-dd",
                    _ => "M/d/yyyy"
                };
                _dateText.Text = now.ToString(dateFormat);
                _dateText.Visibility = Visibility.Visible;
            }
            else
            {
                _dateText.Visibility = Visibility.Collapsed;
            }
        }
    }
}