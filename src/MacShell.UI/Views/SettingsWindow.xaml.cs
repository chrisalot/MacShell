using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MacShell.Core.Interfaces;
using MacShell.Core.Services;

namespace MacShell.UI.Views;

/// <summary>
/// Settings window with navigation.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private readonly DockManager _dockManager;
    private readonly IAppLauncher _appLauncher;
    private readonly ISystemService _systemService;
    private readonly IWindowManager _windowManager;

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

        InitializeComponent();

        // Navigate to General by default
        ContentFrame.Navigate(typeof(GeneralSettingsView), new SettingsContext
        {
            DockManager = _dockManager,
            AppLauncher = _appLauncher,
            SystemService = _systemService,
            WindowManager = _windowManager
        });

        NavView.SelectedItem = NavGeneral;
    }

    private void OnNavItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            NavigateToPage(item.Tag?.ToString() ?? "");
        }
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
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

        switch (tag)
        {
            case "General":
                ContentFrame.Navigate(typeof(GeneralSettingsView), context);
                break;
            case "Dock":
                ContentFrame.Navigate(typeof(DockSettingsView), context);
                break;
            case "Appearance":
                ContentFrame.Navigate(typeof(AppearanceSettingsView), context);
                break;
            case "Widgets":
                ContentFrame.Navigate(typeof(WidgetsSettingsView), context);
                break;
            case "About":
                ContentFrame.Navigate(typeof(AboutSettingsView), context);
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