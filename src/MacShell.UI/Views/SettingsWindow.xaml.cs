using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using MacShell.Core.Interfaces;
using MacShell.Core.Services;
using MacShell.UI.Helpers;

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
    private NavigationViewItem? _navGeneral;
    private NavigationViewItem? _navDock;
    private NavigationViewItem? _navAppearance;
    private NavigationViewItem? _navWidgets;
    private NavigationViewItem? _navAbout;

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

        LoadXaml();
        SetupEventHandlers();

        // Navigate to General by default
        NavigateToPage("General");
        _navView!.SelectedItem = _navGeneral;
    }

    private void LoadXaml()
    {
        // Load XAML from file at runtime
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var assemblyDir = System.IO.Path.GetDirectoryName(assembly.Location) ?? "";
        var xamlPath = System.IO.Path.Combine(assemblyDir, "Views", "SettingsWindow.xaml");

        if (!System.IO.File.Exists(xamlPath))
        {
            throw new System.IO.FileNotFoundException($"SettingsWindow.xaml not found at {xamlPath}");
        }

        var xamlContent = System.IO.File.ReadAllText(xamlPath);
        var window = (Window)XamlReader.Load(xamlContent);

        // Copy properties from loaded window to this instance
        // Note: WinUI 3 Window doesn't have Width, Height, MinWidth, MinHeight - those are on the root element
        this.Title = window.Title;
        this.Content = window.Content;

        // Find named elements from the loaded content
        if (this.Content is FrameworkElement root)
        {
            _navView = root.FindName("NavView") as NavigationView;
            _contentFrame = root.FindName("ContentFrame") as Frame;
            _navGeneral = root.FindName("NavGeneral") as NavigationViewItem;
            _navDock = root.FindName("NavDock") as NavigationViewItem;
            _navAppearance = root.FindName("NavAppearance") as NavigationViewItem;
            _navWidgets = root.FindName("NavWidgets") as NavigationViewItem;
            _navAbout = root.FindName("NavAbout") as NavigationViewItem;
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