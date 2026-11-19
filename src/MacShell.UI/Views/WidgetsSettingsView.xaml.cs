using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using CommunityToolkit.Mvvm.ComponentModel;

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

    public ObservableCollection<WidgetInfo> BuiltInWidgets { get; } = new()
    {
        new WidgetInfo { Name = "Clock", Description = "Shows current time and date" },
        new WidgetInfo { Name = "System Monitor", Description = "CPU, memory, and network usage" },
        new WidgetInfo { Name = "Weather", Description = "Current weather and forecast" }
    };

    public ObservableCollection<WidgetInfo> UserWidgets { get; } = new();
}

/// <summary>
/// Widget info for display.
/// </summary>
public sealed class WidgetInfo
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? Path { get; init; }
}

/// <summary>
/// Widgets settings page.
/// </summary>
public sealed class WidgetsSettingsView : Page
{
    public WidgetsSettingsViewModel ViewModel { get; } = new();

    public WidgetsSettingsView()
    {
        this.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        CreateUIProgrammatically();
        DataContext = ViewModel;
    }

    private void CreateUIProgrammatically()
    {
        var scrollViewer = new ScrollViewer
        {
            Padding = new Thickness(24),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var mainStack = new StackPanel
        {
            Spacing = 24,
            MaxWidth = 560
        };

        // Header
        var headerStack = new StackPanel { Spacing = 8 };
        headerStack.Children.Add(new TextBlock
        {
            Text = "Widgets",
            Style = (Style)Application.Current.Resources["MacShellTextStyleDisplay"]
        });
        headerStack.Children.Add(new TextBlock
        {
            Text = "Manage and configure widgets",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"]
        });
        mainStack.Children.Add(headerStack);

        // Built-in Widgets
        var builtInCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var builtInStack = new StackPanel { Spacing = 16 };
        var builtInHeader = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
        var builtInTitle = new TextBlock
        {
            Text = "Built-in Widgets",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        };
        builtInHeader.Children.Add(builtInTitle);
        Grid.SetColumn(builtInTitle, 0);

        var addWidgetButton = new Button
        {
            Style = (Style)Application.Current.Resources["MacShellAccentButtonStyle"],
            Content = "Add Widget"
        };
        builtInHeader.Children.Add(addWidgetButton);
        Grid.SetColumn(addWidgetButton, 1);
        builtInStack.Children.Add(builtInHeader);

        var builtInList = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            IsItemClickEnabled = true,
            ItemsSource = ViewModel.BuiltInWidgets
        };
        builtInList.ItemTemplate = (DataTemplate)XamlReader.Load(
            "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
            "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">" +
            "<Border Style=\"{StaticResource MacShellCardStyle}\" Padding=\"12\" Margin=\"0,4\">" +
            "<StackPanel Orientation=\"Horizontal\" Spacing=\"16\">" +
            "<FontIcon Glyph=\"&#xE8AF;\" FontSize=\"24\" VerticalAlignment=\"Center\" Foreground=\"{ThemeResource MacShellAccentBrush}\" />" +
            "<StackPanel Spacing=\"4\" VerticalAlignment=\"Center\">" +
            "<TextBlock Text=\"{Binding Name}\" Style=\"{StaticResource MacShellTextStyleBody}\" />" +
            "<TextBlock Text=\"{Binding Description}\" Style=\"{StaticResource MacShellTextStyleCaption}\" />" +
            "</StackPanel>" +
            "</StackPanel>" +
            "</Border>" +
            "</DataTemplate>");
        builtInStack.Children.Add(builtInList);
        builtInCard.Child = builtInStack;
        mainStack.Children.Add(builtInCard);

        // User Widgets
        var userCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var userStack = new StackPanel { Spacing = 16 };
        var userHeader = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
        var userTitle = new TextBlock
        {
            Text = "User Widgets",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        };
        userHeader.Children.Add(userTitle);
        Grid.SetColumn(userTitle, 0);

        var openFolderButton = new Button
        {
            Style = (Style)Application.Current.Resources["MacShellButtonStyle"],
            Content = "Open Folder"
        };
        userHeader.Children.Add(openFolderButton);
        Grid.SetColumn(openFolderButton, 1);
        userStack.Children.Add(userHeader);

        userStack.Children.Add(new TextBlock
        {
            Text = "Place your custom widget folders in the UserWidgets directory. Each widget should contain a widget.json manifest and compiled DLL.",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"],
            TextWrapping = TextWrapping.Wrap
        });

        var userList = new ListView
        {
            SelectionMode = ListViewSelectionMode.None,
            IsItemClickEnabled = true,
            ItemsSource = ViewModel.UserWidgets
        };
        userList.ItemTemplate = (DataTemplate)XamlReader.Load(
            "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
            "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">" +
            "<Border Style=\"{StaticResource MacShellCardStyle}\" Padding=\"12\" Margin=\"0,4\">" +
            "<StackPanel Orientation=\"Horizontal\" Spacing=\"16\">" +
            "<FontIcon Glyph=\"&#xE7EE;\" FontSize=\"24\" VerticalAlignment=\"Center\" Foreground=\"{ThemeResource MacShellTextSecondaryBrush}\" />" +
            "<StackPanel Spacing=\"4\" VerticalAlignment=\"Center\">" +
            "<TextBlock Text=\"{Binding Name}\" Style=\"{StaticResource MacShellTextStyleBody}\" />" +
            "<TextBlock Text=\"{Binding Path}\" Style=\"{StaticResource MacShellTextStyleCaption}\" />" +
            "</StackPanel>" +
            "<StackPanel Orientation=\"Horizontal\" Spacing=\"8\" HorizontalAlignment=\"Right\">" +
            "<Button Style=\"{StaticResource MacShellIconButtonStyle}\" Content=\"&#xE713;\" />" +
            "<Button Style=\"{StaticResource MacShellIconButtonStyle}\" Content=\"&#xE74D;\" />" +
            "</StackPanel>" +
            "</StackPanel>" +
            "</Border>" +
            "</DataTemplate>");
        userStack.Children.Add(userList);
        userCard.Child = userStack;
        mainStack.Children.Add(userCard);

        // Widget Settings
        var settingsCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var settingsStack = new StackPanel { Spacing = 16 };
        settingsStack.Children.Add(new TextBlock
        {
            Text = "Widget Settings",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        var settingsInner = new StackPanel { Spacing = 12 };
        settingsInner.Children.Add(CreateToggleRow("Auto-reload on Change", "Automatically reload widgets when files change",
            ViewModel, nameof(ViewModel.AutoReload)));
        settingsInner.Children.Add(CreateToggleRow("Show Widget Errors", "Display widget load errors in the dock",
            ViewModel, nameof(ViewModel.ShowErrors)));
        settingsStack.Children.Add(settingsInner);
        settingsCard.Child = settingsStack;
        mainStack.Children.Add(settingsCard);

        scrollViewer.Content = mainStack;
        this.Content = scrollViewer;
    }

    private StackPanel CreateToggleRow(string title, string description, WidgetsSettingsViewModel vm, string propertyName)
    {
        var row = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
        var leftStack = new StackPanel { Spacing = 4 };
        leftStack.Children.Add(new TextBlock
        {
            Text = title,
            Style = (Style)Application.Current.Resources["MacShellTextStyleBody"]
        });
        leftStack.Children.Add(new TextBlock
        {
            Text = description,
            Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"]
        });
        row.Children.Add(leftStack);
        Grid.SetColumn(leftStack, 0);

        var toggle = new ToggleSwitch
        {
            Style = (Style)Application.Current.Resources["MacShellToggleSwitchStyle"],
            OnContent = "",
            OffContent = ""
        };
        toggle.SetBinding(ToggleSwitch.IsOnProperty, new Microsoft.UI.Xaml.Data.Binding
        {
            Source = vm,
            Path = new PropertyPath(propertyName),
            Mode = Microsoft.UI.Xaml.Data.BindingMode.TwoWay
        });
        row.Children.Add(toggle);
        Grid.SetColumn(toggle, 1);

        var wrapper = new StackPanel();
        wrapper.Children.Add(row);
        return wrapper;
    }
}