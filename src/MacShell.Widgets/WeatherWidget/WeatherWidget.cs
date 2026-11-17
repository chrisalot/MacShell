using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;
using Microsoft.Extensions.Logging;

namespace MacShell.Widgets.WeatherWidget;

/// <summary>
/// Weather widget showing current weather conditions.
/// Uses Open-Meteo API (free, no key required).
/// </summary>
public sealed class WeatherWidget : IWidget
{
    private readonly ILogger<WeatherWidget> _logger;
    private readonly HttpClient _httpClient;
    private WeatherWidgetView? _view;
    private DispatcherTimer? _timer;
    private WidgetContext? _context;
    private WeatherSettings _settings = new();
    private WeatherData? _currentWeather;

    public WidgetManifest Manifest { get; } = new()
    {
        Id = "com.macshell.weather",
        Name = "Weather Widget",
        Version = "1.0.0",
        Author = "MacShell Team",
        Description = "Shows current weather conditions",
        EntryPoint = "MacShell.Widgets.WeatherWidget.WeatherWidget",
        AssemblyName = "WeatherWidget.dll",
        Category = "Information",
        AllowMultipleInstances = true
    };

    public WeatherWidget(ILogger<WeatherWidget>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<WeatherWidget>.Instance;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task InitializeAsync(WidgetContext context)
    {
        _context = context;

        // Load settings
        if (_context.SettingsService != null)
        {
            var savedSettings = await _context.SettingsService.GetSettingsAsync(Manifest.Id);
            if (savedSettings != null)
            {
                _settings = savedSettings.Data.Deserialize<WeatherSettings>() ?? new WeatherSettings();
            }
        }

        // Create view
        _view = new WeatherWidgetView(this);
        _view.UpdateDisplay(_settings, _currentWeather);

        // Start timer
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(_settings.UpdateInterval, 300000)) };
        _timer.Tick += async (s, e) => await FetchWeatherAsync();
        _timer.Start();

        // Initial fetch
        await FetchWeatherAsync();

        _logger.LogInformation("Weather widget initialized");
    }

    public FrameworkElement GetView()
    {
        return _view ?? new WeatherWidgetView(this);
    }

    public Task OnSettingsChangedAsync(WidgetSettings settings)
    {
        _settings = settings.Data.Deserialize<WeatherSettings>() ?? new WeatherSettings();

        if (_timer != null)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(Math.Max(_settings.UpdateInterval, 300000));
        }

        _view?.UpdateDisplay(_settings, _currentWeather);
        _logger.LogInformation("Weather widget settings updated");
        return Task.CompletedTask;
    }

    public Task ReloadAsync()
    {
        _ = FetchWeatherAsync();
        _logger.LogInformation("Weather widget reloaded");
        return Task.CompletedTask;
    }

    private async Task FetchWeatherAsync()
    {
        try
        {
            double latitude = 0, longitude = 0;

            if (_settings.Location == "auto")
            {
                // Try to get location from IP
                var location = await GetLocationFromIPAsync();
                if (location != null)
                {
                    latitude = location.Latitude;
                    longitude = location.Longitude;
                }
                else
                {
                    // Default to a major city (e.g., New York)
                    latitude = 40.7128;
                    longitude = -74.0060;
                }
            }
            else
            {
                // Parse custom location (simplified - would need geocoding)
                latitude = 40.7128;
                longitude = -74.0060;
            }

            // Fetch from Open-Meteo
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true&temperature_unit={(_settings.Units == "metric" ? "celsius" : "fahrenheit")}&windspeed_unit=kmh&precipitation_unit=mm";

            var response = await _httpClient.GetStringAsync(url);
            var weatherResponse = JsonSerializer.Deserialize<WeatherApiResponse>(response);

            if (weatherResponse?.CurrentWeather != null)
            {
                _currentWeather = new WeatherData
                {
                    Temperature = weatherResponse.CurrentWeather.Temperature,
                    WindSpeed = weatherResponse.CurrentWeather.WindSpeed,
                    WeatherCode = weatherResponse.CurrentWeather.WeatherCode,
                    Time = DateTime.Parse(weatherResponse.CurrentWeather.Time).ToLocalTime()
                };

                _view?.UpdateDisplay(_settings, _currentWeather);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch weather data");
            _currentWeather = null;
            _view?.UpdateDisplay(_settings, null);
        }
    }

    private async Task<GeoLocation?> GetLocationFromIPAsync()
    {
        try
        {
            // Use ip-api.com for free geolocation
            var response = await _httpClient.GetStringAsync("http://ip-api.com/json/");
            var location = JsonSerializer.Deserialize<IpApiResponse>(response);
            if (location?.Status == "success")
            {
                return new GeoLocation { Latitude = location.Lat, Longitude = location.Lon };
            }
        }
        catch { }
        return null;
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer = null;
        _httpClient?.Dispose();
        _view = null;
    }
}

/// <summary>
/// Weather widget settings.
/// </summary>
public sealed class WeatherSettings
{
    public string Location { get; set; } = "auto";
    public string Units { get; set; } = "metric";
    public bool ShowCondition { get; set; } = true;
    public int UpdateInterval { get; set; } = 900000;
}

/// <summary>
/// Current weather data.
/// </summary>
public sealed class WeatherData
{
    public double Temperature { get; set; }
    public double WindSpeed { get; set; }
    public int WeatherCode { get; set; }
    public DateTime Time { get; set; }
}

/// <summary>
/// Open-Meteo API response.
/// </summary>
internal sealed class WeatherApiResponse
{
    public CurrentWeather? CurrentWeather { get; set; }
}

internal sealed class CurrentWeather
{
    public double Temperature { get; set; }
    public double WindSpeed { get; set; }
    public int WeatherCode { get; set; }
    public string Time { get; set; } = "";
}

/// <summary>
/// IP-API response for geolocation.
/// </summary>
internal sealed class IpApiResponse
{
    public string Status { get; set; } = "";
    public double Lat { get; set; }
    public double Lon { get; set; }
}

internal sealed class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}