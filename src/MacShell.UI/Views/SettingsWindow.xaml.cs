using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using MacShell.Core.Interfaces;
using MacShell.Core.Services;
using MacShell.UI.Views;

namespace MacShell.UI.Views;

/// <summary>
/// Settings window with navigation.
/// </summary>
public sealed class SettingsWindow : Window
{
    private readonly DockManager _dockManager;
    private readonly IAppLauncher _appLauncher;
    private readonly ISystemService _systemService;
    private readonly IWindowManager _windowManager;

    private NavigationView? _navView;
    private Frame? _contentFrame;

    public SettingsWindow(
        DockManager dockManager,
        IAppLauncher appLauncher,
        ISystemService systemService,
        IWindowManager windowManager)
    {
        _dockManager = dockManager;
        _appLauncher = appLauncher;
        _systemService = systemService;
        _windowManager = windowManager;

        CreateUIProgrammatically();
        SetupEventHandlers();

        // Configure window size after window is created
        ConfigureWindowSize();

        // Navigate to General by default
        NavigateToPage("General");
        _navView!.SelectedItem = _navView.MenuItems[0];
    }

    private void CreateUIProgrammatically()
    {
        this.Title = "MacShell Settings";

        // Create root grid with background
        var rootGrid = new Grid
        {
            Background = (Brush)Application.Current.Resources["MacShellBackgroundBrush"]
        };

        // Create NavigationView
        _navView = new NavigationView
        {
            Name = "NavView",
            Style = (Style)Application.Current.Resources["MacShellSettingsNavViewStyle"],
            PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
            IsSettingsVisible = false
        };

        // Create navigation menu items
        var navGeneral = new NavigationViewItem
        {
            Name = "NavGeneral",
            Content = "General",
            Icon = new FontIcon { Glyph = "" },
            Tag = "General"
        };
        var navDock = new NavigationViewItem
        {
            Name = "NavDock",
            Content = "Dock",
            Icon = new FontIcon { Glyph = "" },
            Tag = "Dock"
        };
        var navAppearance = new NavigationViewItem
        {
            Name = "NavAppearance",
            Content = "Appearance",
            Icon = new FontIcon { Glyph = "" },
            Tag = "Appearance"
        };
        var navWidgets = new NavigationViewItem
        {
            Name = "NavWidgets",
            Content = "Widgets",
            Icon = new FontIcon { Glyph = "" },
            Tag = "Widgets"
        };
        var navAbout = new NavigationViewItem
        {
            Name = "NavAbout",
            Content = "About",
            Icon = new FontIcon { Glyph = "" },
            Tag = "About"
        };

        _navView.MenuItems.Add(navGeneral);
        _navView.MenuItems.Add(navDock);
        _navView.MenuItems.Add(navAppearance);
        _navView.MenuItems.Add(navWidgets);
        _navView.MenuItems.Add(navAbout);

        // Create content frame
        _contentFrame = new Frame
        {
            Name = "ContentFrame",
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
        };
        _navView.Content = _contentFrame;

        // Set window content
        rootGrid.Children.Add(_navView);
        this.Content = rootGrid;
    }

    private void ConfigureWindowSize()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowIdPtr = Win32Interop.GetWindowIdFromWindow(hwnd);
        var windowId = new Microsoft.UI.WindowId((ulong)windowIdPtr);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        if (appWindow != null)
        {
            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = true;
                presenter.IsMaximizable = true;
                presenter.IsMinimizable = true;
            }
            appWindow.Resize(new Windows.Graphics.SizeInt32(640, 520));
        }
    }

    private void SetupEventHandlers()
    {
        if (_navView != null)
        {
            _navView.ItemInvoked += OnNavItemInvoked;
        }
    }

    private void OnNavItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            NavigateToPage(item.Tag?.ToString() ?? "");
        }
    }

    private void NavigateToPage(string tag)
    {
        var context = new SettingsContext
        {
            DockManager = _dockManager,
            AppLauncher = _appLauncher,
            SystemService = _systemService,
            WindowManager = _windowManager
        };

        if (_contentFrame == null) return;

        switch (tag)
        {
            case "General":
                _contentFrame.Navigate(typeof(GeneralSettingsView), context);
                break;
            case "Dock":
                _contentFrame.Navigate(typeof(DockSettingsView), context);
                break;
            case "Appearance":
                _contentFrame.Navigate(typeof(AppearanceSettingsView), context);
                break;
            case "Widgets":
                _contentFrame.Navigate(typeof(WidgetsSettingsView), context);
                break;
            case "About":
                _contentFrame.Navigate(typeof(AboutSettingsView), context);
                break;
        }
    }
}

/// <summary>
/// Context passed to settings pages.
/// </summary>
internal sealed class SettingsContext
{
    public required DockManager DockManager { get; init; }
    public required IAppLauncher AppLauncher { get; init; }
    public required ISystemService SystemService { get; init; }
    public required IWindowManager WindowManager { get; init; }
}

internal static class Win32Interop
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern IntPtr GetWindowIdFromWindow(IntPtr hWnd);
}