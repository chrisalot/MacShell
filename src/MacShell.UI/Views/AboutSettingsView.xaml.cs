using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MacShell.UI.Views;

/// <summary>
/// About settings page.
/// </summary>
public sealed partial class AboutSettingsView : Page
{
    public AboutSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set version
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString(3)
            ?? "1.0.0";
        VersionText.Text = $"Version {version}";

        // Load system info
        LoadSystemInfo();
    }

    private void LoadSystemInfo()
    {
        SystemInfoPanel.Children.Clear();

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
            SystemInfoPanel.Children.Add(panel);
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