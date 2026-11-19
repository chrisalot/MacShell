using System;
using System.IO;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace MacShell.Widgets.SystemMonitorWidget;

/// <summary>
/// System monitor widget view.
/// </summary>
public sealed class SystemMonitorWidgetView : UserControl
{
    private StackPanel? _rootPanel;
    private StackPanel? _cpuPanel;
    private TextBlock? _cpuValue;
    private ProgressBar? _cpuProgress;
    private StackPanel? _memoryPanel;
    private TextBlock? _memoryValue;
    private ProgressBar? _memoryProgress;
    private StackPanel? _networkPanel;
    private TextBlock? _networkValue;

    public SystemMonitorWidgetView() : this(null) { }

    public SystemMonitorWidgetView(SystemMonitorWidget? widget)
    {
        LoadXaml();
    }

    private void LoadXaml()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? "";
        var xamlPath = Path.Combine(assemblyDir, "SystemMonitorWidgetView.xaml");

        if (!File.Exists(xamlPath))
        {
            throw new FileNotFoundException($"SystemMonitorWidgetView.xaml not found at {xamlPath}");
        }

        var xamlContent = File.ReadAllText(xamlPath);
        var control = (UserControl)XamlReader.Load(xamlContent);

        // Copy properties
        this.Background = control.Background;
        this.Padding = control.Padding;
        this.Content = control.Content;

        // Find named elements
        _rootPanel = FindName("RootPanel") as StackPanel;
        _cpuPanel = FindName("CpuPanel") as StackPanel;
        _cpuValue = FindName("CpuValue") as TextBlock;
        _cpuProgress = FindName("CpuProgress") as ProgressBar;
        _memoryPanel = FindName("MemoryPanel") as StackPanel;
        _memoryValue = FindName("MemoryValue") as TextBlock;
        _memoryProgress = FindName("MemoryProgress") as ProgressBar;
        _networkPanel = FindName("NetworkPanel") as StackPanel;
        _networkValue = FindName("NetworkValue") as TextBlock;
    }

    public void UpdateDisplay(SystemMonitorSettings settings, float cpuUsage, float memoryUsage, long networkSent, long networkReceived)
    {
        // CPU
        if (_cpuPanel != null)
            _cpuPanel.Visibility = settings.ShowCpu ? Visibility.Visible : Visibility.Collapsed;
        if (settings.ShowCpu)
        {
            if (_cpuValue != null)
                _cpuValue.Text = $"{cpuUsage:F0}%";
            if (!settings.CompactMode && _cpuProgress != null)
            {
                _cpuProgress.Value = cpuUsage;
                _cpuProgress.Visibility = Visibility.Visible;
            }
        }

        // Memory
        if (_memoryPanel != null)
            _memoryPanel.Visibility = settings.ShowMemory ? Visibility.Visible : Visibility.Collapsed;
        if (settings.ShowMemory)
        {
            if (_memoryValue != null)
                _memoryValue.Text = $"{memoryUsage:F0}%";
            if (!settings.CompactMode && _memoryProgress != null)
            {
                _memoryProgress.Value = memoryUsage;
                _memoryProgress.Visibility = Visibility.Visible;
            }
        }

        // Network
        if (_networkPanel != null)
            _networkPanel.Visibility = settings.ShowNetwork ? Visibility.Visible : Visibility.Collapsed;
        if (settings.ShowNetwork)
        {
            // Simplified network display
            if (_networkValue != null)
                _networkValue.Text = "0 KB/s";
        }

        // Compact mode adjustments
        if (_rootPanel != null)
        {
            if (settings.CompactMode)
            {
                _rootPanel.Spacing = 8;
                if (_cpuPanel != null) _cpuPanel.Margin = new Thickness(0);
                if (_memoryPanel != null) _memoryPanel.Margin = new Thickness(8, 0, 0, 0);
                if (_networkPanel != null) _networkPanel.Margin = new Thickness(8, 0, 0, 0);
            }
            else
            {
                _rootPanel.Spacing = 4;
            }
        }
    }
}