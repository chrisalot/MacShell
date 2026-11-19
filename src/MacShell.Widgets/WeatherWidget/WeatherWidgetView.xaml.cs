using System;
using System.IO;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace MacShell.Widgets.WeatherWidget;

/// <summary>
/// Weather widget view.
/// </summary>
public sealed class WeatherWidgetView : UserControl
{
    private StackPanel? _rootPanel;
    private FontIcon? _weatherIcon;
    private TextBlock? _tempText;
    private TextBlock? _conditionText;

    public WeatherWidgetView() : this(null) { }

    public WeatherWidgetView(WeatherWidget? widget)
    {
        LoadXaml();
    }

    private void LoadXaml()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? "";
        var xamlPath = Path.Combine(assemblyDir, "WeatherWidgetView.xaml");

        if (!File.Exists(xamlPath))
        {
            throw new FileNotFoundException($"WeatherWidgetView.xaml not found at {xamlPath}");
        }

        var xamlContent = File.ReadAllText(xamlPath);
        var control = (UserControl)XamlReader.Load(xamlContent);

        // Copy properties
        this.Background = control.Background;
        this.Padding = control.Padding;
        this.Content = control.Content;

        // Find named elements
        _rootPanel = FindName("RootPanel") as StackPanel;
        _weatherIcon = FindName("WeatherIcon") as FontIcon;
        _tempText = FindName("TempText") as TextBlock;
        _conditionText = FindName("ConditionText") as TextBlock;
    }

    public void UpdateDisplay(WeatherSettings settings, WeatherData? weather)
    {
        if (weather != null)
        {
            // Temperature
            var unit = settings.Units == "metric" ? "°C" : "°F";
            if (_tempText != null)
                _tempText.Text = $"{weather.Temperature:F0}{unit}";

            // Weather icon based on WMO weather code
            if (_weatherIcon != null)
                _weatherIcon.Glyph = GetWeatherIcon(weather.WeatherCode);

            // Condition text
            if (settings.ShowCondition)
            {
                if (_conditionText != null)
                {
                    _conditionText.Text = GetWeatherCondition(weather.WeatherCode);
                    _conditionText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (_conditionText != null)
                    _conditionText.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            if (_tempText != null)
                _tempText.Text = "--°";
            if (_weatherIcon != null)
                _weatherIcon.Glyph = "&#xE946;"; // Cloud/unknown
            if (_conditionText != null)
            {
                _conditionText.Text = "Unavailable";
                _conditionText.Visibility = settings.ShowCondition ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    private static string GetWeatherIcon(int code)
    {
        // WMO Weather codes: https://www.nodc.noaa.gov/archive/arc0021/0002199/1.1/data/0-data/HTML/WMO-CODE/WMO4677.HTM
        return code switch
        {
            0 => "&#xE9C8;",           // Clear sky
            1 or 2 or 3 => "&#xE9C9;", // Mainly clear, partly cloudy, overcast
            45 or 48 => "&#xE9CA;",    // Fog
            51 or 53 or 55 => "&#xE9CB;", // Drizzle
            56 or 57 => "&#xE9CC;",    // Freezing drizzle
            61 or 63 or 65 => "&#xE9CD;", // Rain
            66 or 67 => "&#xE9CE;",    // Freezing rain
            71 or 73 or 75 => "&#xE9CF;", // Snow
            77 => "&#xE9D0;",          // Snow grains
            80 or 81 or 82 => "&#xE9D1;", // Rain showers
            85 or 86 => "&#xE9D2;",    // Snow showers
            95 => "&#xE9D3;",          // Thunderstorm
            96 or 99 => "&#xE9D4;",    // Thunderstorm with hail
            _ => "&#xE946;"            // Default cloud
        };
    }

    private static string GetWeatherCondition(int code)
    {
        return code switch
        {
            0 => "Clear sky",
            1 => "Mainly clear",
            2 => "Partly cloudy",
            3 => "Overcast",
            45 => "Fog",
            48 => "Depositing rime fog",
            51 => "Light drizzle",
            53 => "Moderate drizzle",
            55 => "Dense drizzle",
            56 => "Light freezing drizzle",
            57 => "Dense freezing drizzle",
            61 => "Slight rain",
            63 => "Moderate rain",
            65 => "Heavy rain",
            66 => "Light freezing rain",
            67 => "Heavy freezing rain",
            71 => "Slight snow fall",
            73 => "Moderate snow fall",
            75 => "Heavy snow fall",
            77 => "Snow grains",
            80 => "Slight rain showers",
            81 => "Moderate rain showers",
            82 => "Violent rain showers",
            85 => "Slight snow showers",
            86 => "Heavy snow showers",
            95 => "Thunderstorm",
            96 => "Thunderstorm with slight hail",
            99 => "Thunderstorm with heavy hail",
            _ => "Unknown"
        };
    }
}