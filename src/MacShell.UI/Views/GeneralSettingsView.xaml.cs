using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MacShell.UI.Views;

/// <summary>
/// View model for General settings.
/// </summary>
public sealed partial class GeneralSettingsViewModel : ObservableObject
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
    public void ChangeLauncherHotkey()
    {
        // TODO: Implement hotkey change dialog
    }

    [RelayCommand]
    public void ChangeSettingsHotkey()
    {
        // TODO: Implement hotkey change dialog
    }
}

/// <summary>
/// General settings page.
/// </summary>
public sealed class GeneralSettingsView : Page
{
    public GeneralSettingsViewModel ViewModel { get; } = new();

    public GeneralSettingsView()
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
            Text = "General",
            Style = (Style)Application.Current.Resources["MacShellTextStyleDisplay"]
        });
        headerStack.Children.Add(new TextBlock
        {
            Text = "Configure MacShell startup and behavior",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"]
        });
        mainStack.Children.Add(headerStack);

        // Start at Login
        mainStack.Children.Add(CreateCard("Start at Login", "Automatically launch MacShell when you sign in to Windows",
            CreateToggleSwitch(ViewModel, nameof(ViewModel.StartAtLogin))));

        // Restore Window Position
        mainStack.Children.Add(CreateCard("Restore Window Position", "Remember and restore window position on startup",
            CreateToggleSwitch(ViewModel, nameof(ViewModel.RestorePosition))));

        // Check for Updates
        mainStack.Children.Add(CreateCard("Check for Updates", "Automatically check for MacShell updates",
            CreateToggleSwitch(ViewModel, nameof(ViewModel.CheckForUpdates))));

        // Hotkeys Section
        var hotkeysCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var hotkeysStack = new StackPanel { Spacing = 16 };
        hotkeysStack.Children.Add(new TextBlock
        {
            Text = "Global Hotkeys",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });

        var hotkeysInner = new StackPanel { Spacing = 12 };
        hotkeysInner.Children.Add(CreateHotkeyRow("Open Launcher", "Win + Space", "Change", ViewModel.ChangeLauncherHotkeyCommand));
        hotkeysInner.Children.Add(CreateHotkeyRow("Open Settings", "Win + ,", "Change", ViewModel.ChangeSettingsHotkeyCommand));
        hotkeysStack.Children.Add(hotkeysInner);
        hotkeysCard.Child = hotkeysStack;
        mainStack.Children.Add(hotkeysCard);

        // Advanced
        var advancedCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var advancedStack = new StackPanel { Spacing = 16 };
        advancedStack.Children.Add(new TextBlock
        {
            Text = "Advanced",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        var advancedInner = new StackPanel { Spacing = 12 };
        advancedInner.Children.Add(CreateToggleRow("Enable Debug Logging", "Log detailed information for troubleshooting",
            ViewModel, nameof(ViewModel.EnableDebugLogging)));
        advancedInner.Children.Add(CreateToggleRow("High Performance Mode", "Reduce animations and effects for better performance",
            ViewModel, nameof(ViewModel.HighPerformanceMode)));
        advancedStack.Children.Add(advancedInner);
        advancedCard.Child = advancedStack;
        mainStack.Children.Add(advancedCard);

        scrollViewer.Content = mainStack;
        this.Content = scrollViewer;
    }

    private Border CreateCard(string title, string description, UIElement content)
    {
        var card = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var stack = new StackPanel { Spacing = 12 };
        var header = CreateSpaceBetweenPanel(
            new TextBlock
            {
                Text = title,
                Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
            },
            new TextBlock
            {
                Text = description,
                Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"]
            },
            content);
        stack.Children.Add(header);
        card.Child = stack;
        return card;
    }

    private ToggleSwitch CreateToggleSwitch(GeneralSettingsViewModel vm, string propertyName)
    {
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
        return toggle;
    }

    private StackPanel CreateHotkeyRow(string label, string hotkey, string buttonText, IRelayCommand command)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12
        };
        var leftStack = new StackPanel { Spacing = 4 };
        leftStack.Children.Add(new TextBlock
        {
            Text = label,
            Style = (Style)Application.Current.Resources["MacShellTextStyleBody"]
        });
        leftStack.Children.Add(new TextBlock
        {
            Text = hotkey,
            Style = (Style)Application.Current.Resources["MacShellTextStyleCaption"],
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["MacShellTextTertiaryBrush"]
        });
        row.Children.Add(leftStack);

        // Add spacer
        var spacer = new Border { Width = 10 };
        row.Children.Add(spacer);

        var button = new Button
        {
            Style = (Style)Application.Current.Resources["MacShellButtonStyle"],
            Content = buttonText,
            Command = command
        };
        row.Children.Add(button);
        return row;
    }

    private StackPanel CreateToggleRow(string title, string description, GeneralSettingsViewModel vm, string propertyName)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12
        };
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

        // Add spacer
        var spacer = new Border { Width = 10 };
        row.Children.Add(spacer);

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
        return row;
    }

    /// <summary>
    /// Creates a horizontal panel with left content, right content, and optional action element (space-between layout).
    /// </summary>
    private StackPanel CreateSpaceBetweenPanel(UIElement leftContent, UIElement rightContent, UIElement? actionElement = null)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        panel.Children.Add(leftContent);
        if (actionElement != null)
        {
            // Add flexible spacer
            var spacer = new Border { Width = 10 };
            panel.Children.Add(spacer);
            panel.Children.Add(actionElement);
        }
        else
        {
            // Add flexible spacer
            var spacer = new Border { Width = 10 };
            panel.Children.Add(spacer);
            panel.Children.Add(rightContent);
        }
        return panel;
    }
}