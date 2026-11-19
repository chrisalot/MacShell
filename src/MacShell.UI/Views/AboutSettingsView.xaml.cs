using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MacShell.UI.Views;

/// <summary>
/// About settings page.
/// </summary>
public sealed class AboutSettingsView : Page
{
    private TextBlock? _versionText;
    private StackPanel? _systemInfoPanel;

    public AboutSettingsView()
    {
        this.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        CreateUIProgrammatically();
        Loaded += OnLoaded;
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
            MaxWidth = 560,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // App Icon and Name
        var iconStack = new StackPanel { Spacing = 16, HorizontalAlignment = HorizontalAlignment.Center };
        var iconBorder = new Border
        {
            Width = 120,
            Height = 120,
            CornerRadius = new CornerRadius(24),
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["MacShellAccentBrush"],
            HorizontalAlignment = HorizontalAlignment.Center
        };
        iconBorder.Child = new FontIcon
        {
            Glyph = "",
            FontSize = 48,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        iconStack.Children.Add(iconBorder);

        var nameStack = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
        nameStack.Children.Add(new TextBlock
        {
            Text = "MacShell",
            Style = (Style)Application.Current.Resources["MacShellTextStyleDisplay"],
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        });
        _versionText = new TextBlock
        {
            Style = (Style)Application.Current.Resources["MacShellTextStyleBodySecondary"]
        };
        nameStack.Children.Add(_versionText);
        iconStack.Children.Add(nameStack);
        mainStack.Children.Add(iconStack);

        // Description
        var descCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        descCard.Child = new TextBlock
        {
            Text = "A Mac-inspired desktop shell for Windows. Built with WinUI 3 for native performance and a premium experience.",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBody"],
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        };
        mainStack.Children.Add(descCard);

        // Links
        var linksCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var linksStack = new StackPanel { Spacing = 12 };
        linksStack.Children.Add(new TextBlock
        {
            Text = "Links",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        var linksButtonStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16, HorizontalAlignment = HorizontalAlignment.Center };
        var githubButton = new Button { Style = (Style)Application.Current.Resources["MacShellButtonStyle"], Content = "GitHub" };
        var docsButton = new Button { Style = (Style)Application.Current.Resources["MacShellButtonStyle"], Content = "Documentation" };
        var issueButton = new Button { Style = (Style)Application.Current.Resources["MacShellButtonStyle"], Content = "Report Issue" };
        githubButton.Click += OnGitHubClick;
        docsButton.Click += OnDocsClick;
        issueButton.Click += OnIssueClick;
        linksButtonStack.Children.Add(githubButton);
        linksButtonStack.Children.Add(docsButton);
        linksButtonStack.Children.Add(issueButton);
        linksStack.Children.Add(linksButtonStack);
        linksCard.Child = linksStack;
        mainStack.Children.Add(linksCard);

        // License
        var licenseCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var licenseStack = new StackPanel { Spacing = 12 };
        licenseStack.Children.Add(new TextBlock
        {
            Text = "License",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        licenseStack.Children.Add(new TextBlock
        {
            Text = "MIT License - Free for personal and commercial use.",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBody"],
            TextWrapping = TextWrapping.Wrap
        });
        var licenseButton = new Button
        {
            Style = (Style)Application.Current.Resources["MacShellButtonStyle"],
            Content = "View License",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        licenseButton.Click += OnLicenseClick;
        licenseStack.Children.Add(licenseButton);
        licenseCard.Child = licenseStack;
        mainStack.Children.Add(licenseCard);

        // Acknowledgments
        var ackCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var ackStack = new StackPanel { Spacing = 12 };
        ackStack.Children.Add(new TextBlock
        {
            Text = "Acknowledgments",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        ackStack.Children.Add(new TextBlock
        {
            Text = "Built with WinUI 3, Windows App SDK, and Community Toolkit.",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBody"],
            TextWrapping = TextWrapping.Wrap
        });
        ackStack.Children.Add(new TextBlock
        {
            Text = "Icons from Segoe Fluent Icons.",
            Style = (Style)Application.Current.Resources["MacShellTextStyleBody"],
            TextWrapping = TextWrapping.Wrap
        });
        ackCard.Child = ackStack;
        mainStack.Children.Add(ackCard);

        // System Info
        var sysCard = new Border
        {
            Style = (Style)Application.Current.Resources["MacShellCardStyle"]
        };
        var sysStack = new StackPanel { Spacing = 8 };
        sysStack.Children.Add(new TextBlock
        {
            Text = "System Information",
            Style = (Style)Application.Current.Resources["MacShellTextStyleTitle"]
        });
        _systemInfoPanel = new StackPanel { Spacing = 4 };
        sysStack.Children.Add(_systemInfoPanel);
        sysCard.Child = sysStack;
        mainStack.Children.Add(sysCard);

        scrollViewer.Content = mainStack;
        this.Content = scrollViewer;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set version
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString(3)
            ?? "1.0.0";
        if (_versionText != null)
            _versionText.Text = $"Version {version}";

        // Load system info
        LoadSystemInfo();
    }

    private void LoadSystemInfo()
    {
        if (_systemInfoPanel == null) return;

        _systemInfoPanel.Children.Clear();

        var infoItems = new[]
        {
            ("OS", RuntimeInformation.OSDescription),
            ("Architecture", RuntimeInformation.OSArchitecture.ToString()),
            ("Framework", RuntimeInformation.FrameworkDescription),
            ("Process Architecture", RuntimeInformation.ProcessArchitecture.ToString()),
        };

        foreach (var (label, value) in infoItems)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            panel.Children.Add(new TextBlock
            {
                Text = label,
                Style = (Style)Application.Current.Resources["MacShellTextStyleCaption"],
                Width = 140,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["MacShellTextTertiaryBrush"]
            });
            panel.Children.Add(new TextBlock
            {
                Text = value,
                Style = (Style)Application.Current.Resources["MacShellTextStyleCaption"],
                TextWrapping = TextWrapping.Wrap
            });
            _systemInfoPanel.Children.Add(panel);
        }
    }

    private void OnGitHubClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/macshell/macshell") { UseShellExecute = true });
    }

    private void OnDocsClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/macshell/macshell/wiki") { UseShellExecute = true });
    }

    private void OnIssueClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/macshell/macshell/issues/new") { UseShellExecute = true });
    }

    private void OnLicenseClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/macshell/macshell/blob/main/LICENSE") { UseShellExecute = true });
    }
}