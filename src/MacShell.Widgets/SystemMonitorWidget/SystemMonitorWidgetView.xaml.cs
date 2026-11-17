using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MacShell.Widgets.SystemMonitorWidget;

/// <summary>
/// System monitor widget view.
/// </summary>
public sealed partial class SystemMonitorWidgetView : UserControl
{
    public SystemMonitorWidgetView()
    {
        InitializeComponent();
    }

    public void UpdateDisplay(SystemMonitorSettings settings, float cpuUsage, float memoryUsage, long networkSent, long networkReceived)
    {
        // CPU
        CpuPanel.Visibility = settings.ShowCpu ? Visibility.Visible : Visibility.Collapsed;
        if (settings.ShowCpu)
        {
            CpuValue.Text = $"{cpuUsage:F0}%";
            if (!settings.CompactMode)
            {
                CpuProgress.Value = cpuUsage;
                CpuProgress.Visibility = Visibility.Visible;
            }
        }

        // Memory
        MemoryPanel.Visibility = settings.ShowMemory ? Visibility.Visible : Visibility.Collapsed;
        if (settings.ShowMemory)
        {
            MemoryValue.Text = $"{memoryUsage:F0}%";
            if (!settings.CompactMode)
            {
                MemoryProgress.Value = memoryUsage;
                MemoryProgress.Visibility = Visibility.Visible;
            }
        }

        // Network
        NetworkPanel.Visibility = settings.ShowNetwork ? Visibility.Visible : Visibility.Collapsed;
        if (settings.ShowNetwork)
        {
            // Simplified network display
            NetworkValue.Text = "0 KB/s";
        }

        // Compact mode adjustments
        if (settings.CompactMode)
        {
            RootPanel.Spacing = 8;
            CpuPanel.Margin = new Thickness(0);
            MemoryPanel.Margin = new Thickness(8, 0, 0, 0);
            NetworkPanel.Margin = new Thickness(8, 0, 0, 0);
        }
        else
        {
            RootPanel.Spacing = 4;
        }
    }
}