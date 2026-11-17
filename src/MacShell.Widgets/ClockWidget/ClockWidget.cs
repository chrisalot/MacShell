using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;
using Microsoft.Extensions.Logging;

namespace MacShell.Widgets.ClockWidget;

/// <summary>
/// Clock widget showing current time and date.
/// </summary>
public sealed class ClockWidget : IWidget
{
    private readonly ILogger<ClockWidget> _logger;
    private ClockWidgetView? _view;
    private DispatcherTimer? _timer;
    private WidgetContext? _context;
    private ClockSettings _settings = new();

    public WidgetManifest Manifest { get; } = new()
    {
        Id = "com.macshell.clock",
        Name = "Clock Widget",
        Version = "1.0.0",
        Author = "MacShell Team",
        Description = "Displays current time and date",
        EntryPoint = "MacShell.Widgets.ClockWidget.ClockWidget",
        AssemblyName = "ClockWidget.dll",
        Category = "System",
        AllowMultipleInstances = true
    };

    public ClockWidget(ILogger<ClockWidget>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ClockWidget>.Instance;
    }

    public Task InitializeAsync(WidgetContext context)
    {
        _context = context;

        // Load settings
        if (_context.SettingsService != null)
        {
            var savedSettings = _context.SettingsService.GetSettingsAsync(Manifest.Id).Result;
            if (savedSettings != null)
            {
                _settings = savedSettings.Data.Deserialize<ClockSettings>() ?? new ClockSettings();
            }
        }

        // Create view
        _view = new ClockWidgetView(this);
        _view.UpdateTime(_settings);

        // Start timer
        _timer = new DispatcherTimer
        {
            Interval = _settings.ShowSeconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(30)
        };
        _timer.Tick += (s, e) => _view?.UpdateTime(_settings);
        _timer.Start();

        // Subscribe to system time changes
        _context.SystemService.TimeChanged += OnSystemTimeChanged;

        _logger.LogInformation("Clock widget initialized");
        return Task.CompletedTask;
    }

    public FrameworkElement GetView()
    {
        return _view ?? new ClockWidgetView(this);
    }

    public Task OnSettingsChangedAsync(WidgetSettings settings)
    {
        _settings = settings.Data.Deserialize<ClockSettings>() ?? new ClockSettings();

        if (_timer != null)
        {
            _timer.Interval = _settings.ShowSeconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(30);
        }

        _view?.UpdateTime(_settings);
        _logger.LogInformation("Clock widget settings updated");
        return Task.CompletedTask;
    }

    public Task ReloadAsync()
    {
        _logger.LogInformation("Clock widget reloaded");
        return Task.CompletedTask;
    }

    private void OnSystemTimeChanged(object? sender, DateTime time)
    {
        _view?.UpdateTime(_settings);
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer = null;

        if (_context?.SystemService != null)
        {
            _context.SystemService.TimeChanged -= OnSystemTimeChanged;
        }

        _view = null;
    }
}

/// <summary>
/// Clock widget settings.
/// </summary>
public sealed class ClockSettings
{
    public string Format { get; set; } = "24h"; // "12h" or "24h"
    public bool ShowSeconds { get; set; } = false;
    public bool ShowDate { get; set; } = true;
    public string DateFormat { get; set; } = "short"; // "short", "long", "iso"
}