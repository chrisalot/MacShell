using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using MacShell.Core.Models;

namespace MacShell.UI.Views;

/// <summary>
/// View model for Widgets settings.
/// </summary>
public sealed partial class WidgetsSettingsViewModel : ObservableObject
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
public sealed partial class WidgetInfo : ObservableObject
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
public sealed class WidgetsSettingsView : Page
{
    public WidgetsSettingsViewModel ViewModel { get; } = new();

    private ListView? _builtInWidgetsList;
    private ListView? _userWidgetsList;
    private Button? _addWidgetButton;
    private Button? _openUserWidgetsFolderButton;
    private Button? _configureWidgetButton;
    private Button? _removeWidgetButton;
    private ToggleSwitch? _autoReloadToggle;
    private ToggleSwitch? _showErrorsToggle;

    public WidgetsSettingsView()
    {
        LoadXaml();
        SetupEventHandlers();
        DataContext = ViewModel;
    }

    private void LoadXaml()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? "";
        var xamlPath = Path.Combine(assemblyDir, "Views", "WidgetsSettingsView.xaml");

        if (!File.Exists(xamlPath))
        {
            throw new FileNotFoundException($"WidgetsSettingsView.xaml not found at {xamlPath}");
        }

        var xamlContent = File.ReadAllText(xamlPath);
        var page = (Page)XamlReader.Load(xamlContent);

        // Copy properties
        this.Background = page.Background;
        this.Content = page.Content;

        // Find named elements
        _builtInWidgetsList = FindName("BuiltInWidgetsList") as ListView;
        _userWidgetsList = FindName("UserWidgetsList") as ListView;
        _addWidgetButton = FindName("AddWidgetButton") as Button;
        _openUserWidgetsFolderButton = FindName("OpenUserWidgetsFolderButton") as Button;
        _configureWidgetButton = FindName("ConfigureWidgetButton") as Button;
        _removeWidgetButton = FindName("RemoveWidgetButton") as Button;
        _autoReloadToggle = FindName("AutoReloadToggle") as ToggleSwitch;
        _showErrorsToggle = FindName("ShowErrorsToggle") as ToggleSwitch;

        // Bind toggles to ViewModel
        if (_autoReloadToggle != null)
            _autoReloadToggle.IsOn = ViewModel.AutoReload;
        if (_showErrorsToggle != null)
            _showErrorsToggle.IsOn = ViewModel.ShowErrors;
    }

    private void SetupEventHandlers()
    {
        if (_addWidgetButton != null)
            _addWidgetButton.Click += OnAddWidget;
        if (_openUserWidgetsFolderButton != null)
            _openUserWidgetsFolderButton.Click += OnOpenUserWidgetsFolder;
        if (_configureWidgetButton != null)
            _configureWidgetButton.Click += OnConfigureWidget;
        if (_removeWidgetButton != null)
            _removeWidgetButton.Click += OnRemoveWidget;
        if (_builtInWidgetsList != null)
            _builtInWidgetsList.ItemClick += OnWidgetItemClick;
        if (_userWidgetsList != null)
            _userWidgetsList.ItemClick += OnUserWidgetItemClick;
        if (_autoReloadToggle != null)
            _autoReloadToggle.Toggled += (s, e) => ViewModel.AutoReload = _autoReloadToggle.IsOn;
        if (_showErrorsToggle != null)
            _showErrorsToggle.Toggled += (s, e) => ViewModel.ShowErrors = _showErrorsToggle.IsOn;
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