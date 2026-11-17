using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using MacShell.Core.Models;

namespace MacShell.UI.Views;

/// <summary>
/// View model for Widgets settings.
/// </summary>
internal sealed partial class WidgetsSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _autoReload = true;

    [ObservableProperty]
    private bool _showErrors = true;

    [ObservableProperty]
    private ObservableCollection<WidgetInfo> _builtInWidgets = new();

    [ObservableProperty]
    private ObservableCollection<WidgetInfo> _userWidgets = new();
}

/// <summary>
/// Widget info for display in settings.
/// </summary>
internal sealed partial class WidgetInfo : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private string _path = "";

    [ObservableProperty]
    private bool _isEnabled = true;
}

/// <summary>
/// Widgets settings page.
/// </summary>
public sealed partial class WidgetsSettingsView : Page
{
    public WidgetsSettingsViewModel ViewModel { get; } = new();

    public WidgetsSettingsView()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is SettingsContext context)
        {
            LoadWidgets();
        }
    }

    private void LoadWidgets()
    {
        // Load built-in widgets
        ViewModel.BuiltInWidgets.Clear();
        ViewModel.BuiltInWidgets.Add(new WidgetInfo
        {
            Name = "Clock",
            Description = "Shows current time and date",
            Path = "Built-in"
        });
        ViewModel.BuiltInWidgets.Add(new WidgetInfo
        {
            Name = "System Monitor",
            Description = "CPU, memory, and network usage",
            Path = "Built-in"
        });
        ViewModel.BuiltInWidgets.Add(new WidgetInfo
        {
            Name = "Weather",
            Description = "Current weather conditions",
            Path = "Built-in"
        });

        // Load user widgets (scan folder)
        LoadUserWidgets();
    }

    private void LoadUserWidgets()
    {
        ViewModel.UserWidgets.Clear();
        var userWidgetsPath = GetUserWidgetsPath();
        if (System.IO.Directory.Exists(userWidgetsPath))
        {
            foreach (var dir in System.IO.Directory.GetDirectories(userWidgetsPath))
            {
                var manifestPath = System.IO.Path.Combine(dir, "widget.json");
                if (System.IO.File.Exists(manifestPath))
                {
                    try
                    {
                        var json = System.IO.File.ReadAllText(manifestPath);
                        var manifest = MacShell.Core.Models.WidgetManifest.FromJson(json);
                        ViewModel.UserWidgets.Add(new WidgetInfo
                        {
                            Name = manifest.Name,
                            Description = manifest.Description,
                            Path = dir
                        });
                    }
                    catch { }
                }
            }
        }
    }

    private static string GetUserWidgetsPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return System.IO.Path.Combine(localAppData, "MacShell", "UserWidgets");
    }

    private void OnAddWidget(object sender, RoutedEventArgs e)
    {
        // Open user widgets folder
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = GetUserWidgetsPath(),
            UseShellExecute = true,
            Verb = "open"
        });
    }

    private void OnOpenUserWidgetsFolder(object sender, RoutedEventArgs e)
    {
        OnAddWidget(sender, e);
    }

    private void OnWidgetItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is WidgetInfo widget)
        {
            // Show widget configuration
        }
    }

    private void OnUserWidgetItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is WidgetInfo widget)
        {
            // Show user widget configuration
        }
    }

    private void OnConfigureWidget(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is WidgetInfo widget)
        {
            // Open widget configuration dialog
        }
    }

    private void OnRemoveWidget(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is WidgetInfo widget)
        {
            // Remove widget (delete folder)
        }
    }
}