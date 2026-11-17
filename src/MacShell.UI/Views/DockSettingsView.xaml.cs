using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MacShell.UI.Views;

/// <summary>
/// View model for Dock settings.
/// </summary>
internal sealed partial class DockSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _iconSize = 48;

    [ObservableProperty]
    private bool _enableMagnification = true;

    [ObservableProperty]
    private double _magnificationFactor = 1.4;

    [ObservableProperty]
    private bool _enableAnimations = true;

    [ObservableProperty]
    private bool _showRunningIndicators = true;

    [ObservableProperty]
    private bool _autoHide = false;

    [ObservableProperty]
    private int _autoHideDelay = 200;
}

/// <summary>
/// Dock settings page.
/// </summary>
public sealed partial class DockSettingsView : Page
{
    public DockSettingsViewModel ViewModel { get; } = new();

    public DockSettingsView()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is SettingsContext context)
        {
            // Load settings from context.DockManager
        }
    }

    private void OnManagePinnedApps(object sender, RoutedEventArgs e)
    {
        // TODO: Open pinned apps management dialog
    }
}