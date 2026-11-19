using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MacShell.UI.Views;

/// <summary>
/// View model for Dock settings.
/// </summary>
public sealed partial class DockSettingsViewModel : ObservableObject
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
public sealed class DockSettingsView : Page
{
    public DockSettingsViewModel ViewModel { get; } = new();

    private TextBlock? _iconSizeText;
    private TextBlock? _magnificationFactorText;
    private TextBlock? _autoHideDelayText;
    private StackPanel? _magnificationPanel;
    private StackPanel? _autoHidePanel;

    public DockSettingsView()
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
            Text = "Dock",
            Style = (Style)Application.Current.Resources["MacShellTextStyleDisplay"]
        });
        headerStack.Children.Add(new TextBlock
        {
            Text = "Customize dock appearance and behavior",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"]
        });
        mainStack.Children.Add(headerStack);

        // Position
        var positionCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var positionStack = new StackPanel { Spacing = 16 };
        positionStack.Children.Add(new TextBlock
        {
            Text = "Position",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        var positionRadioStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        var posBottom = new RadioButton
        {
            Content = "Bottom",
            GroupName = "DockPosition",
            IsChecked = true,
            Tag = "Bottom",
            Style = (Style)Application.Current.Resources["MacShellRadioButtonStyle"]
        };
        var posLeft = new RadioButton
        {
            Content = "Left",
            GroupName = "DockPosition",
            Tag = "Left",
            Style = (Style)Application.Current.Resources["MacShellRadioButtonStyle"]
        };
        var posRight = new RadioButton
        {
            Content = "Right",
            GroupName = "DockPosition",
            Tag = "Right",
            Style = (Style)Application.Current.Resources["MacShellRadioButtonStyle"]
        };
        positionRadioStack.Children.Add(posBottom);
        positionRadioStack.Children.Add(posLeft);
        positionRadioStack.Children.Add(posRight);
        positionStack.Children.Add(positionRadioStack);
        positionCard.Child = positionStack;
        mainStack.Children.Add(positionCard);

        // Size
        var sizeCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var sizeStack = new StackPanel { Spacing = 16 };
        sizeStack.Children.Add(CreateHeaderWithControl("Icon Size",
            new TextBlock
            {
                Name = "IconSizeText",
                Text = "48",
                Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"]
            },
            CreateSlider(32, 64, 4, 48, 200, (value) =>
            {
                if (_iconSizeText != null)
                    _iconSizeText.Text = $"{(int)value}";
                ViewModel.IconSize = (int)value;
            })));
        sizeCard.Child = sizeStack;
        mainStack.Children.Add(sizeCard);

        // Magnification
        var magCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var magStack = new StackPanel { Spacing = 16 };
        magStack.Children.Add(CreateHeaderWithToggle("Magnification", "Enlarge icons on hover",
            ViewModel, nameof(ViewModel.EnableMagnification), (isOn) =>
            {
                if (_magnificationPanel != null)
                    _magnificationPanel.Visibility = isOn ? Visibility.Visible : Visibility.Collapsed;
            }));

        _magnificationPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Visibility = Visibility.Collapsed
        };
        _magnificationPanel.Children.Add(new TextBlock
        {
            Text = "Magnification Factor",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBody"]
        });
        _magnificationFactorText = new TextBlock
        {
            Text = "1.4",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"]
        };
        _magnificationPanel.Children.Add(_magnificationFactorText);

        var magFactorSlider = CreateSlider(1.1, 2.0, 0.1, 1.4, 200, (value) =>
        {
            if (_magnificationFactorText != null)
                _magnificationFactorText.Text = $"{value:F1}";
            ViewModel.MagnificationFactor = value;
        });
        _magnificationPanel.Children.Add(magFactorSlider);
        magStack.Children.Add(_magnificationPanel);

        magCard.Child = magStack;
        mainStack.Children.Add(magCard);

        // Animation
        var animCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var animStack = new StackPanel { Spacing = 16 };
        animStack.Children.Add(CreateToggleRow("Animations", "Smooth transitions and effects",
            ViewModel, nameof(ViewModel.EnableAnimations)));
        animCard.Child = animStack;
        mainStack.Children.Add(animCard);

        // Running Indicators
        var runningCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var runningStack = new StackPanel { Spacing = 16 };
        runningStack.Children.Add(CreateToggleRow("Running Indicators", "Show dots under running applications",
            ViewModel, nameof(ViewModel.ShowRunningIndicators)));
        runningCard.Child = runningStack;
        mainStack.Children.Add(runningCard);

        // Auto-hide
        var autoHideCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var autoHideStack = new StackPanel { Spacing = 16 };
        autoHideStack.Children.Add(CreateHeaderWithToggle("Auto-hide Dock", "Automatically hide dock when not in use",
            ViewModel, nameof(ViewModel.AutoHide), (isOn) =>
            {
                if (_autoHidePanel != null)
                    _autoHidePanel.Visibility = isOn ? Visibility.Visible : Visibility.Collapsed;
            }));

        _autoHidePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Visibility = Visibility.Collapsed
        };
        _autoHidePanel.Children.Add(new TextBlock
        {
            Text = "Show Delay",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBody"]
        });
        _autoHideDelayText = new TextBlock
        {
            Text = "200 ms",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"]
        };
        _autoHidePanel.Children.Add(_autoHideDelayText);

        var autoHideDelaySlider = CreateSlider(0, 1000, 50, 200, 200, (value) =>
        {
            if (_autoHideDelayText != null)
                _autoHideDelayText.Text = $"{(int)value} ms";
            ViewModel.AutoHideDelay = (int)value;
        });
        _autoHidePanel.Children.Add(autoHideDelaySlider);
        autoHideStack.Children.Add(_autoHidePanel);

        autoHideCard.Child = autoHideStack;
        mainStack.Children.Add(autoHideCard);

        // Pinned Apps Management
        var pinnedCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var pinnedStack = new StackPanel { Spacing = 16 };
        pinnedStack.Children.Add(new TextBlock
        {
            Text = "Pinned Applications",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        var manageButton = new Button
        {
            Style = (Style)Application.Current.Resources["MacShellAccentButtonStyle"],
            Content = "Manage Pinned Apps"
        };
        manageButton.Click += OnManagePinnedApps;
        pinnedStack.Children.Add(manageButton);
        pinnedCard.Child = pinnedStack;
        mainStack.Children.Add(pinnedCard);

        scrollViewer.Content = mainStack;
        this.Content = scrollViewer;
    }

    private StackPanel CreateHeaderWithControl(string title, FrameworkElement textElement, Slider slider)
    {
        var stack = new StackPanel { Spacing = 12 };
        var header = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
        var leftStack = new StackPanel { Spacing = 4 };
        leftStack.Children.Add(new TextBlock
        {
            Text = title,
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        Grid.SetColumn(leftStack, 0);
        header.Children.Add(leftStack);
        Grid.SetColumn(slider, 1);
        header.Children.Add(slider);
        stack.Children.Add(header);

        if (textElement != null)
        {
            var textRow = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
            Grid.SetColumn(textElement, 0);
            textRow.Children.Add(textElement);
            stack.Children.Add(textRow);
        }
        return stack;
    }

    private StackPanel CreateHeaderWithToggle(string title, string description, DockSettingsViewModel vm, string propertyName, Action<bool>? onToggled = null)
    {
        var stack = new StackPanel { Spacing = 12 };
        var header = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
        var leftStack = new StackPanel { Spacing = 4 };
        leftStack.Children.Add(new TextBlock
        {
            Text = title,
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        leftStack.Children.Add(new TextBlock
        {
            Text = description,
            Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"]
        });
        Grid.SetColumn(leftStack, 0);
        header.Children.Add(leftStack);

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
        if (onToggled != null)
            toggle.Toggled += (s, e) => onToggled(((ToggleSwitch)s).IsOn);
        Grid.SetColumn(toggle, 1);
        header.Children.Add(toggle);
        stack.Children.Add(header);
        return stack;
    }

    private StackPanel CreateToggleRow(string title, string description, DockSettingsViewModel vm, string propertyName)
    {
        var row = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
        var leftStack = new StackPanel { Spacing = 4 };
        leftStack.Children.Add(new TextBlock
        {
            Text = title,
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        leftStack.Children.Add(new TextBlock
        {
            Text = description,
            Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"]
        });
        Grid.SetColumn(leftStack, 0);
        row.Children.Add(leftStack);

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
        Grid.SetColumn(toggle, 1);
        row.Children.Add(toggle);

        var wrapper = new StackPanel();
        wrapper.Children.Add(row);
        return wrapper;
    }

    private Slider CreateSlider(double minimum, double maximum, double stepFrequency, double value, double width, Action<double> onValueChanged)
    {
        var slider = new Slider
        {
            Style = (Style)Application.Current.Resources["MacShellSliderStyle"],
            Minimum = minimum,
            Maximum = maximum,
            StepFrequency = stepFrequency,
            Value = value,
            Width = width
        };
        slider.ValueChanged += (s, e) => onValueChanged(e.NewValue);
        return slider;
    }

    private void OnManagePinnedApps(object sender, RoutedEventArgs e)
    {
        // TODO: Open pinned apps management dialog
    }
}