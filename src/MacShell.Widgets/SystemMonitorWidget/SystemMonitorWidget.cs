using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;
using Microsoft.Extensions.Logging;

namespace MacShell.Widgets.SystemMonitorWidget;

/// <summary>
/// System monitor widget showing CPU, memory, and network usage.
/// </summary>
public sealed class SystemMonitorWidget : IWidget
{
    private readonly ILogger<SystemMonitorWidget> _logger;
    private SystemMonitorWidgetView? _view;
    private DispatcherTimer? _timer;
    private WidgetContext? _context;
    private SystemMonitorSettings _settings = new();
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _memoryCounter;

    public WidgetManifest Manifest { get; } = new()
    {
        Id = "com.macshell.systemmonitor",
        Name = "System Monitor Widget",
        Version = "1.0.0",
        Author = "MacShell Team",
        Description = "Shows CPU, memory, and network usage",
        EntryPoint = "MacShell.Widgets.SystemMonitorWidget.SystemMonitorWidget",
        AssemblyName = "SystemMonitorWidget.dll",
        Category = "System",
        AllowMultipleInstances = false
    };

    public SystemMonitorWidget(ILogger<SystemMonitorWidget>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SystemMonitorWidget>.Instance;
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
                _settings = savedSettings.Data.Deserialize<SystemMonitorSettings>() ?? new SystemMonitorSettings();
            }
        }

        // Initialize performance counters
        InitializePerformanceCounters();

        // Create view
        _view = new SystemMonitorWidgetView(this);
        _view.UpdateDisplay(_settings, 0, 0, 0, 0);

        // Start timer
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_settings.UpdateInterval) };
        _timer.Tick += async (s, e) => await UpdateMetricsAsync();
        _timer.Start();

        // Initial update
        _ = UpdateMetricsAsync();

        _logger.LogInformation("System Monitor widget initialized");
        return Task.CompletedTask;
    }

    private void InitializePerformanceCounters()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes", true);
            // Prime the counters
            _cpuCounter.NextValue();
            _memoryCounter.NextValue();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize performance counters");
        }
    }

    public FrameworkElement GetView()
    {
        return _view ?? new SystemMonitorWidgetView(this);
    }

    public Task OnSettingsChangedAsync(WidgetSettings settings)
    {
        _settings = settings.Data.Deserialize<SystemMonitorSettings>() ?? new SystemMonitorSettings();

        if (_timer != null)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(_settings.UpdateInterval);
        }

        _view?.UpdateDisplay(_settings, 0, 0, 0, 0);
        _logger.LogInformation("System Monitor widget settings updated");
        return Task.CompletedTask;
    }

    public Task ReloadAsync()
    {
        InitializePerformanceCounters();
        _ = UpdateMetricsAsync();
        _logger.LogInformation("System Monitor widget reloaded");
        return Task.CompletedTask;
    }

    private async Task UpdateMetricsAsync()
    {
        try
        {
            float cpuUsage = 0;
            float memoryUsage = 0;
            long networkSent = 0;
            long networkReceived = 0;

            if (_cpuCounter != null)
            {
                cpuUsage = _cpuCounter.NextValue();
            }

            if (_memoryCounter != null)
            {
                var availableMB = _memoryCounter.NextValue();
                var totalMB = GetTotalMemoryMB();
                if (totalMB > 0)
                {
                    memoryUsage = 100 - (availableMB / totalMB * 100);
                }
            }

            // Network stats (simplified)
            var networkInfo = _context?.SystemService.Network;
            // Could add network speed calculation here

            _view?.UpdateDisplay(_settings, cpuUsage, memoryUsage, networkSent, networkReceived);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update system metrics");
        }
    }

    private static float GetTotalMemoryMB()
    {
        try
        {
            var counter = new PerformanceCounter("Memory", "Available MBytes", true);
            return 0; // Simplified - would need total memory counter
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer = null;

        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();

        _view = null;
    }
}

/// <summary>
/// System monitor widget settings.
/// </summary>
public sealed class SystemMonitorSettings
{
    public bool ShowCpu { get; set; } = true;
    public bool ShowMemory { get; set; } = true;
    public bool ShowNetwork { get; set; } = true;
    public int UpdateInterval { get; set; } = 2000;
    public bool CompactMode { get; set; } = false;
}