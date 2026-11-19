using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Numerics;
using MacShell.Core.Models;

namespace MacShell.UI.Controls;

/// <summary>
/// A single dock item with icon, running indicator, tooltip, and context menu.
/// </summary>
public sealed class DockItemControl : Control
{
    private DockPanel? _parentDock;
    private Image? _iconImage;
    private Ellipse? _runningIndicator;
    private Border? _backgroundBorder;
    private ToolTip? _toolTip;
    private MenuFlyout? _contextMenu;
    private bool _isPointerOver;

    public DockItemControl()
    {
        DefaultStyleKey = typeof(DockItemControl);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        PointerPressed += OnPointerPressed;
        PointerReleased += OnPointerReleased;
        RightTapped += OnRightTapped;
    }

    #region IconSource Property

    public ImageSource? IconSource
    {
        get => (ImageSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register(nameof(IconSource), typeof(ImageSource), typeof(DockItemControl),
            new PropertyMetadata(null));

    #endregion

    #region IconSize Property

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(DockItemControl),
            new PropertyMetadata(28.0));

    #endregion

    #region IsRunning Property

    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public static readonly DependencyProperty IsRunningProperty =
        DependencyProperty.Register(nameof(IsRunning), typeof(bool), typeof(DockItemControl),
            new PropertyMetadata(false, OnIsRunningChanged));

    private static void OnIsRunningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockItemControl control)
        {
            control.UpdateRunningIndicator();
        }
    }

    #endregion

    #region RunningIndicatorSize Property

    public double RunningIndicatorSize
    {
        get => (double)GetValue(RunningIndicatorSizeProperty);
        set => SetValue(RunningIndicatorSizeProperty, value);
    }

    public static readonly DependencyProperty RunningIndicatorSizeProperty =
        DependencyProperty.Register(nameof(RunningIndicatorSize), typeof(double), typeof(DockItemControl),
            new PropertyMetadata(8.0));

    #endregion

    #region RunningIndicatorColor Property

    public Brush RunningIndicatorColor
    {
        get => (Brush)GetValue(RunningIndicatorColorProperty);
        set => SetValue(RunningIndicatorColorProperty, value);
    }

    public static readonly DependencyProperty RunningIndicatorColorProperty =
        DependencyProperty.Register(nameof(RunningIndicatorColor), typeof(Brush), typeof(DockItemControl),
            new PropertyMetadata(null));

    #endregion

    #region ToolTipTemplate Property

    public DataTemplate? ToolTipTemplate
    {
        get => (DataTemplate?)GetValue(ToolTipTemplateProperty);
        set => SetValue(ToolTipTemplateProperty, value);
    }

    public static readonly DependencyProperty ToolTipTemplateProperty =
        DependencyProperty.Register(nameof(ToolTipTemplate), typeof(DataTemplate), typeof(DockItemControl),
            new PropertyMetadata(null, OnToolTipTemplateChanged));

    private static void OnToolTipTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockItemControl control)
        {
            control.UpdateToolTip();
        }
    }

    #endregion

    #region CornerRadius Property

    public new CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static new readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(DockItemControl),
            new PropertyMetadata(new CornerRadius(8)));

    #endregion

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _iconImage = GetTemplateChild("IconImage") as Image;
        _runningIndicator = GetTemplateChild("RunningIndicator") as Ellipse;
        _backgroundBorder = GetTemplateChild("BackgroundBorder") as Border;

        UpdateIcon();
        UpdateRunningIndicator();
        UpdateToolTip();
        CreateContextMenu();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Find parent DockPanel
        var parent = VisualTreeHelper.GetParent(this);
        while (parent != null)
        {
            if (parent is DockPanel dock)
            {
                _parentDock = dock;
                break;
            }
            parent = VisualTreeHelper.GetParent(parent);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _parentDock = null;
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = true;
        VisualStateManager.GoToState(this, "PointerOver", true);
        _parentDock?.OnItemPointerEntered(this);
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = false;
        VisualStateManager.GoToState(this, "Normal", true);
        _parentDock?.OnItemPointerExited(this);
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "Pressed", true);
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isPointerOver)
        {
            VisualStateManager.GoToState(this, "PointerOver", true);
        }
        else
        {
            VisualStateManager.GoToState(this, "Normal", true);
        }

        // Check if it's a left click
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            _parentDock?.OnItemClicked(this);
        }
    }

    private void OnRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        ShowContextMenu(e);
        _parentDock?.OnItemRightClicked(this);
    }

    private void UpdateIcon()
    {
        if (_iconImage != null && DataContext is DockItem item)
        {
            try
            {
                if (!string.IsNullOrEmpty(item.IconPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.UriSource = new Uri(item.IconPath);
                    bitmap.DecodePixelWidth = (int)IconSize;
                    _iconImage.Source = bitmap;
                }
            }
            catch
            {
                // Fallback to default icon
                _iconImage.Source = null;
            }
        }
    }

    private void UpdateRunningIndicator()
    {
        if (_runningIndicator != null)
        {
            _runningIndicator.Visibility = IsRunning ? Visibility.Visible : Visibility.Collapsed;
            _runningIndicator.Width = RunningIndicatorSize;
            _runningIndicator.Height = RunningIndicatorSize;

            if (RunningIndicatorColor != null)
            {
                _runningIndicator.Fill = RunningIndicatorColor;
            }
        }
    }

    private void UpdateToolTip()
    {
        if (ToolTipTemplate != null && DataContext is DockItem item)
        {
            _toolTip = new ToolTip
            {
                Content = item,
                ContentTemplate = ToolTipTemplate,
            };
            ToolTipService.SetToolTip(this, _toolTip);
            // ToolTipService.Placement and VerticalOffset are not available in current WinUI 3
        }
    }

    private void CreateContextMenu()
    {
        _contextMenu = new MenuFlyout();

        // Pin/Unpin
        var pinItem = new MenuFlyoutItem
        {
            Text = (DataContext as DockItem)?.IsPinned == true ? "Unpin from Dock" : "Pin to Dock",
            Icon = new FontIcon { Glyph = (DataContext as DockItem)?.IsPinned == true ? "" : "" }
        };
        pinItem.Click += (s, e) => TogglePin();
        _contextMenu.Items.Add(pinItem);

        // Open (if running)
        if (DataContext is DockItem item && item.IsRunning)
        {
            var openItem = new MenuFlyoutItem
            {
                Text = "Open",
                Icon = new FontIcon { Glyph = "" }
            };
            openItem.Click += (s, e) => ActivateWindow();
            _contextMenu.Items.Add(openItem);
        }

        _contextMenu.Items.Add(new MenuFlyoutSeparator());

        // Show in Explorer
        var explorerItem = new MenuFlyoutItem
        {
            Text = "Show in Explorer",
            Icon = new FontIcon { Glyph = "" }
        };
        explorerItem.Click += (s, e) => ShowInExplorer();
        _contextMenu.Items.Add(explorerItem);

        // Quit (if running)
        var dockItem = DataContext as DockItem;
        if (dockItem?.IsRunning == true)
        {
            _contextMenu.Items.Add(new MenuFlyoutSeparator());

            var quitItem = new MenuFlyoutItem
            {
                Text = "Quit",
                Icon = new FontIcon { Glyph = "" }
            };
            quitItem.Click += (s, e) => QuitApplication();
            _contextMenu.Items.Add(quitItem);
        }
    }

    private void ShowContextMenu(RightTappedRoutedEventArgs e)
    {
        if (_contextMenu != null)
        {
            _contextMenu.ShowAt(this, e.GetPosition(this));
        }
    }

    private void TogglePin()
    {
        // This would be handled by the parent DockPanel/ViewModel
        // For now, just raise an event
    }

    private void ActivateWindow()
    {
        // Handled by parent
    }

    private void ShowInExplorer()
    {
        if (DataContext is DockItem item && !string.IsNullOrEmpty(item.ExecutablePath))
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{item.ExecutablePath}\"",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch
            {
                // Ignore
            }
        }
    }

    private void QuitApplication()
    {
        if (DataContext is DockItem item && item.ProcessId.HasValue)
        {
            try
            {
                var process = System.Diagnostics.Process.GetProcessById(item.ProcessId.Value);
                process.CloseMainWindow();
            }
            catch
            {
                // Ignore
            }
        }
    }

    private DockItem? item => DataContext as DockItem;
}