using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MacShell.UI.Views;

/// <summary>
/// View model for General settings.
/// </summary>
internal sealed partial class GeneralSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _startAtLogin = true;

    [ObservableProperty]
    private bool _restorePosition = true;

    [ObservableProperty]
    private bool _checkForUpdates = true;

    [ObservableProperty]
    private bool _enableDebugLogging = false;

    [ObservableProperty]
    private bool _highPerformanceMode = false;

    [RelayCommand]
    private void ChangeLauncherHotkey()
    {
        // TODO: Implement hotkey change dialog
    }

    [RelayCommand]
    private void ChangeSettingsHotkey()
    {
        // TODO: Implement hotkey change dialog
    }
}

/// <summary>
/// General settings page.
/// </summary>
public sealed partial class GeneralSettingsView : Page
{
    public GeneralSettingsViewModel ViewModel { get; } = new();

    public GeneralSettingsView()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is SettingsContext context)
        {
            // Load settings from context
        }
    }
}