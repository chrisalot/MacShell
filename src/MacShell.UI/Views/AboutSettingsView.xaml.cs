using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace MacShell.UI.Views;

/// <summary>
/// About settings page.
/// </summary>
public sealed class AboutSettingsView : Page
{
    private TextBlock? _versionText;
    private StackPanel? _systemInfoPanel;
    private Button? _githubButton;
    private Button? _docsButton;
    private Button? _issueButton;
    private Button? _licenseButton;

    public AboutSettingsView()
    {
        LoadXaml();
        SetupEventHandlers();
        Loaded += OnLoaded;
    }

    private void LoadXaml()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? "";
        var xamlPath = Path.Combine(assemblyDir, "Views", "AboutSettingsView.xaml");

        if (!File.Exists(xamlPath))
        {
            throw new FileNotFoundException($"AboutSettingsView.xaml not found at {xamlPath}");
        }

        var xamlContent = File.ReadAllText(xamlPath);
        var page = (Page)XamlReader.Load(xamlContent);

        // Copy properties
        this.Background = page.Background;
        this.Content = page.Content;

        // Find named elements
        _versionText = FindName("VersionText") as TextBlock;
        _systemInfoPanel = FindName("SystemInfoPanel") as StackPanel;
        _githubButton = FindName("GitHubButton") as Button;
        _docsButton = FindName("DocsButton") as Button;
        _issueButton = FindName("IssueButton") as Button;
        _licenseButton = FindName("LicenseButton") as Button;
    }

    private void SetupEventHandlers()
    {
        if (_githubButton != null)
            _githubButton.Click += OnGitHubClick;
        if (_docsButton != null)
            _docsButton.Click += OnDocsClick;
        if (_issueButton != null)
            _issueButton.Click += OnIssueClick;
        if (_licenseButton != null)
            _licenseButton.Click += OnLicenseClick;
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
            panel.Children.Add(new TextBlock { Text = label, Style = (Style)Resources["MacShellTextStyleCaption"], Width = 140, Foreground = (Microsoft.UI.Xaml.Media.Brush)Resources["MacShellTextTertiaryBrush"] });
            panel.Children.Add(new TextBlock { Text = value, Style = (Style)Resources["MacShellTextStyleCaption"], TextWrapping = TextWrapping.Wrap });
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