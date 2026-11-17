using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;
using MacShell.Core.Services;
using MacShell.UI.Views;

namespace MacShell.App;

/// <summary>
/// Main application class.
/// </summary>
public sealed partial class App : Application
{
    private IHost? _host;
    private ShellWindow? _mainWindow;
    private static readonly Mutex _singleInstanceMutex = new(true, "Global\\MacShell_SingleInstance");

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Check for single instance
        if (!_singleInstanceMutex.WaitOne(0, false))
        {
            // Another instance is running - activate it
            ActivateExistingInstance();
            Current.Exit();
            return;
        }

        try
        {
            // Build host with DI
            _host = CreateHostBuilder().Build();
            await _host.StartAsync();

            // Get services
            var dockManager = _host.Services.GetRequiredService<DockManager>();
            var appLauncher = _host.Services.GetRequiredService<IAppLauncher>();
            var systemService = _host.Services.GetRequiredService<ISystemService>();
            var windowManager = _host.Services.GetRequiredService<IWindowManager>();

            // Create and show main window
            _mainWindow = new ShellWindow(dockManager, appLauncher, systemService, windowManager);
            _mainWindow.Activate();

            // Register for restart
            RegisterForRestart();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start MacShell: {ex.Message}");
            Current.Exit();
        }
    }

    private IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Core services
                services.AddSingleton<ISystemService, SystemMonitor>();
                services.AddSingleton<IAppLauncher, AppLauncherService>();
                services.AddSingleton<IWindowManager, WindowManagerService>();
                services.AddSingleton<IDockItemFactory, DockItemFactory>();
                services.AddSingleton<IWidgetSettingsService, WidgetSettingsService>();
                services.AddSingleton<DockManager>();

                // Widget manager
                services.AddSingleton<WidgetManager>();

                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });
    }

    private void ActivateExistingInstance()
    {
        // Find and activate existing window
        // This would use FindWindow or similar Win32 API
        System.Diagnostics.Debug.WriteLine("MacShell is already running");
    }

    private void RegisterForRestart()
    {
        // Register for automatic restart on crash
        try
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                RegisterApplicationRestart(exePath, RestartFlags.None);
            }
        }
        catch { }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Exception}");
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.StopAsync().GetAwaiter().GetResult();
        _host?.Dispose();
        _singleInstanceMutex.ReleaseMutex();
        base.OnExit(e);
    }

    // Win32 P/Invoke for restart registration
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterApplicationRestart(string pwzCommandLine, RestartFlags dwFlags);

    [Flags]
    private enum RestartFlags : uint
    {
        None = 0,
        NoCrash = 1,
        NoHang = 2,
        NoPatch = 4,
        NoReboot = 8
    }
}