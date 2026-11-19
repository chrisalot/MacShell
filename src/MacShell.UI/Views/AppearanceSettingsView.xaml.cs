using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MacShell.UI.Views;

/// <summary>
/// View model for Appearance settings.
/// </summary>
public sealed partial class AppearanceSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _theme = "Dark"; // Dark, Light, System

    [ObservableProperty]
    private string _accentColor = "#007AFF";

    [ObservableProperty]
    private bool _enableTransparency = true;

    [ObservableProperty]
    private int _transparencyLevel = 60;

    [ObservableProperty]
    private int _cornerRadius = 8; // 4, 8, 12, 16

    [ObservableProperty]
    private bool _enableShadows = true;

    [ObservableProperty]
    private bool _followSystemScaling = true;
}

/// <summary>
/// Appearance settings page.
/// </summary>
public sealed class AppearanceSettingsView : Page
{
    public AppearanceSettingsViewModel ViewModel { get; } = new();

    private Border? _accentColorPreview;
    private StackPanel? _transparencyLevelPanel;

    public AppearanceSettingsView()
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
            Text = "Appearance",
            Style = (Style)Application.Current.Resources["MacShellTextStyleDisplay"]
        });
        headerStack.Children.Add(new TextBlock
        {
            Text = "Customize MacShell look and feel",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"]
        });
        mainStack.Children.Add(headerStack);

        // Theme
        var themeCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var themeStack = new StackPanel { Spacing = 16 };
        themeStack.Children.Add(new TextBlock
        {
            Text = "Theme",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        var themeRadioStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
        var themeDark = new RadioButton { Content = "Dark", GroupName = "Theme", IsChecked = true, Tag = "Dark", Style = (Style)Application.Current.Resources["MacShellRadioButtonStyle"] };
        var themeLight = new RadioButton { Content = "Light", GroupName = "Theme", Tag = "Light", Style = (Style)Application.Current.Resources["MacShellRadioButtonStyle"] };
        var themeSystem = new RadioButton { Content = "System", GroupName = "Theme", Tag = "System", Style = (Style)Application.Current.Resources["MacShellRadioButtonStyle"] };
        themeDark.Checked += (s, e) => ViewModel.Theme = "Dark";
        themeLight.Checked += (s, e) => ViewModel.Theme = "Light";
        themeSystem.Checked += (s, e) => ViewModel.Theme = "System";
        themeRadioStack.Children.Add(themeDark);
        themeRadioStack.Children.Add(themeLight);
        themeRadioStack.Children.Add(themeSystem);
        themeStack.Children.Add(themeRadioStack);
        themeCard.Child = themeStack;
        mainStack.Children.Add(themeCard);

        // Accent Color
        var accentCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var accentStack = new StackPanel { Spacing = 16 };
        accentStack.Children.Add(new TextBlock
        {
            Text = "Accent Color",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        var accentRow = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } } };

        _accentColorPreview = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(8),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(255, 0, 122, 255)),
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["MacShellBorderBrush"],
            BorderThickness = new Thickness(1)
        };
        Grid.SetColumn(_accentColorPreview, 0);
        accentRow.Children.Add(_accentColorPreview);

        var accentCombo = new ComboBox
        {
            Style = (Style)Application.Current.Resources["MacShellComboBoxStyle"],
            Width = 200
        };
        accentCombo.Items.Add(new ComboBoxItem { Tag = "#007AFF", Content = "Blue (Default)", IsSelected = true });
        accentCombo.Items.Add(new ComboBoxItem { Tag = "#FF9F0A", Content = "Orange" });
        accentCombo.Items.Add(new ComboBoxItem { Tag = "#FF3B30", Content = "Red" });
        accentCombo.Items.Add(new ComboBoxItem { Tag = "#34C759", Content = "Green" });
        accentCombo.Items.Add(new ComboBoxItem { Tag = "#5E5CE6", Content = "Purple" });
        accentCombo.Items.Add(new ComboBoxItem { Tag = "#FF2D92", Content = "Pink" });
        accentCombo.Items.Add(new ComboBoxItem { Tag = "#00B5C0", Content = "Teal" });
        accentCombo.Items.Add(new ComboBoxItem { Tag = "#FF6D00", Content = "Amber" });
        accentCombo.SelectionChanged += OnAccentColorChanged;
        Grid.SetColumn(accentCombo, 1);
        accentRow.Children.Add(accentCombo);
        accentStack.Children.Add(accentRow);
        accentCard.Child = accentStack;
        mainStack.Children.Add(accentCard);

        // Transparency
        var transCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var transStack = new StackPanel { Spacing = 16 };
        transStack.Children.Add(CreateHeaderWithToggle("Transparency Effects", "Enable acrylic and transparency effects",
            ViewModel, nameof(ViewModel.EnableTransparency), (isOn) =>
            {
                if (_transparencyLevelPanel != null)
                    _transparencyLevelPanel.Visibility = isOn ? Visibility.Visible : Visibility.Collapsed;
            }));

        _transparencyLevelPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Visibility = Visibility.Collapsed
        };
        _transparencyLevelPanel.Children.Add(new TextBlock
        {
            Text = "Transparency Level",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBody"]
        });
        var transLevelText = new TextBlock
        {
            Text = "60%",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"]
        };
        _transparencyLevelPanel.Children.Add(transLevelText);

        var transSlider = CreateSlider(20, 90, 5, 60, 200, (value) =>
        {
            transLevelText.Text = $"{(int)value}%";
            ViewModel.TransparencyLevel = (int)value;
        });
        _transparencyLevelPanel.Children.Add(transSlider);
        transStack.Children.Add(_transparencyLevelPanel);

        transCard.Child = transStack;
        mainStack.Children.Add(transCard);

        // Corner Radius
        var cornerCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var cornerStack = new StackPanel { Spacing = 16 };
        cornerStack.Children.Add(new TextBlock
        {
            Text = "Corner Radius",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        var cornerRadioStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        var cornerSmall = new RadioButton { Content = "Small (4px)", GroupName = "CornerRadius", Tag = "4" };
        var cornerMedium = new RadioButton { Content = "Medium (8px)", GroupName = "CornerRadius", IsChecked = true, Tag = "8" };
        var cornerLarge = new RadioButton { Content = "Large (12px)", GroupName = "CornerRadius", Tag = "12" };
        var cornerRound = new RadioButton { Content = "Round (16px)", GroupName = "CornerRadius", Tag = "16" };
        cornerSmall.Checked += (s, e) => ViewModel.CornerRadius = 4;
        cornerMedium.Checked += (s, e) => ViewModel.CornerRadius = 8;
        cornerLarge.Checked += (s, e) => ViewModel.CornerRadius = 12;
        cornerRound.Checked += (s, e) => ViewModel.CornerRadius = 16;
        cornerRadioStack.Children.Add(cornerSmall);
        cornerRadioStack.Children.Add(cornerMedium);
        cornerRadioStack.Children.Add(cornerLarge);
        cornerRadioStack.Children.Add(cornerRound);
        cornerStack.Children.Add(cornerRadioStack);
        cornerCard.Child = cornerStack;
        mainStack.Children.Add(cornerCard);

        // Shadows
        var shadowCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var shadowStack = new StackPanel { Spacing = 16 };
        shadowStack.Children.Add(CreateToggleRow("Shadows", "Show drop shadows on dock and panels",
            ViewModel, nameof(ViewModel.EnableShadows)));
        shadowCard.Child = shadowStack;
        mainStack.Children.Add(shadowCard);

        // Font Scaling
        var scalingCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var scalingStack = new StackPanel { Spacing = 16 };
        scalingStack.Children.Add(CreateToggleRow("System Font Scaling", "Follow Windows display scaling settings",
            ViewModel, nameof(ViewModel.FollowSystemScaling)));
        scalingCard.Child = scalingStack;
        mainStack.Children.Add(scalingCard);

        scrollViewer.Content = mainStack;
        this.Content = scrollViewer;
    }

    private StackPanel CreateHeaderWithToggle(string title, string description, AppearanceSettingsViewModel vm, string propertyName, Action<bool>? onToggled = null)
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

    private StackPanel CreateToggleRow(string title, string description, AppearanceSettingsViewModel vm, string propertyName)
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

    private void OnAccentColorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item && item.Tag is string colorHex)
        {
            ViewModel.AccentColor = colorHex;
            if (_accentColorPreview != null)
            {
                var color = Microsoft.UI.ColorHelper.FromArgb(
                    255,
                    Convert.ToByte(colorHex.Substring(1, 2), 16),
                    Convert.ToByte(colorHex.Substring(3, 2), 16),
                    Convert.ToByte(colorHex.Substring(5, 2), 16));
                _accentColorPreview.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
            }
        }
    }
}