using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using MacShell.Core.Interfaces;
using MacShell.Core.Models;

namespace MacShell.UI.Controls;

/// <summary>
/// Spotlight-style application launcher overlay.
/// </summary>
public sealed class LauncherWindow : Control
{
    private AutoSuggestBox? _searchBox;
    private ListView? _resultsList;
    private Border? _panelBorder;
    private Compositor? _compositor;
    private SpriteVisual? _overlayVisual;
    private IAppLauncher? _appLauncher;
    private DispatcherTimer? _debounceTimer;
    private bool _isOpen;

    public LauncherWindow()
    {
        DefaultStyleKey = typeof(LauncherWindow);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #region AppLauncher Property

    public IAppLauncher? AppLauncher
    {
        get => (IAppLauncher?)GetValue(AppLauncherProperty);
        set => SetValue(AppLauncherProperty, value);
    }

    public static readonly DependencyProperty AppLauncherProperty =
        DependencyProperty.Register(nameof(AppLauncher), typeof(IAppLauncher), typeof(LauncherWindow),
            new PropertyMetadata(null, OnAppLauncherChanged));

    private static void OnAppLauncherChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LauncherWindow launcher)
        {
            launcher._appLauncher = e.NewValue as IAppLauncher;
        }
    }

    #endregion

    #region OverlayBrush Property

    public Brush OverlayBrush
    {
        get => (Brush)GetValue(OverlayBrushProperty);
        set => SetValue(OverlayBrushProperty, value);
    }

    public static readonly DependencyProperty OverlayBrushProperty =
        DependencyProperty.Register(nameof(OverlayBrush), typeof(Brush), typeof(LauncherWindow),
            new PropertyMetadata(null, OnOverlayBrushChanged));

    private static void OnOverlayBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LauncherWindow launcher && launcher._overlayVisual != null)
        {
            launcher._overlayVisual.Brush = launcher.OverlayBrush as CompositionBrush ??
                launcher._compositor!.CreateColorBrush(Colors.Black);
        }
    }

    #endregion

    #region PanelBackground Property

    public Brush PanelBackground
    {
        get => (Brush)GetValue(PanelBackgroundProperty);
        set => SetValue(PanelBackgroundProperty, value);
    }

    public static readonly DependencyProperty PanelBackgroundProperty =
        DependencyProperty.Register(nameof(PanelBackground), typeof(Brush), typeof(LauncherWindow),
            new PropertyMetadata(null));

    #endregion

    #region PanelCornerRadius Property

    public CornerRadius PanelCornerRadius
    {
        get => (CornerRadius)GetValue(PanelCornerRadiusProperty);
        set => SetValue(PanelCornerRadiusProperty, value);
    }

    public static readonly DependencyProperty PanelCornerRadiusProperty =
        DependencyProperty.Register(nameof(PanelCornerRadius), typeof(CornerRadius), typeof(LauncherWindow),
            new PropertyMetadata(new CornerRadius(16)));

    #endregion

    #region PanelWidth Property

    public double PanelWidth
    {
        get => (double)GetValue(PanelWidthProperty);
        set => SetValue(PanelWidthProperty, value);
    }

    public static readonly DependencyProperty PanelWidthProperty =
        DependencyProperty.Register(nameof(PanelWidth), typeof(double), typeof(LauncherWindow),
            new PropertyMetadata(640.0));

    #endregion

    #region PanelMaxHeight Property

    public double PanelMaxHeight
    {
        get => (double)GetValue(PanelMaxHeightProperty);
        set => SetValue(PanelMaxHeightProperty, value);
    }

    public static readonly DependencyProperty PanelMaxHeightProperty =
        DependencyProperty.Register(nameof(PanelMaxHeight), typeof(double), typeof(LauncherWindow),
            new PropertyMetadata(480.0));

    #endregion

    #region Shadow Property

    public DropShadow? Shadow
    {
        get => (DropShadow?)GetValue(ShadowProperty);
        set => SetValue(ShadowProperty, value);
    }

    public static readonly DependencyProperty ShadowProperty =
        DependencyProperty.Register(nameof(Shadow), typeof(DropShadow), typeof(LauncherWindow),
            new PropertyMetadata(null));

    #endregion

    #region IsOpen Property

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(LauncherWindow),
            new PropertyMetadata(false, OnIsOpenChanged));

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LauncherWindow launcher)
        {
            if ((bool)e.NewValue)
            {
                launcher.Show();
            }
            else
            {
                launcher.Hide();
            }
        }
    }

    #endregion

    #region Events

    public event EventHandler? Opened;
    public event EventHandler? Closed;
    public event EventHandler<AppInfo>? AppLaunched;

    #endregion

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _searchBox = GetTemplateChild("SearchBox") as AutoSuggestBox;
        _resultsList = GetTemplateChild("ResultsList") as ListView;
        _panelBorder = GetTemplateChild("PanelBorder") as Border;

        if (_searchBox != null)
        {
            _searchBox.TextChanged += OnSearchTextChanged;
            _searchBox.QuerySubmitted += OnQuerySubmitted;
            _searchBox.KeyDown += OnSearchKeyDown;
            _searchBox.LostFocus += OnSearchLostFocus;
        }

        if (_resultsList != null)
        {
            _resultsList.SelectionChanged += OnSelectionChanged;
            _resultsList.KeyDown += OnResultsKeyDown;
        }

        InitializeComposition();
    }

    private void InitializeComposition()
    {
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

        // Create overlay visual for dimming background
        _overlayVisual = _compositor.CreateSpriteVisual();
        _overlayVisual.Size = new Vector2((float)ActualWidth, (float)ActualHeight);
        _overlayVisual.Brush = _compositor.CreateColorBrush(Colors.Black);
        _overlayVisual.Opacity = 0f;
        ElementCompositionPreview.SetElementChildVisual(this, _overlayVisual);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SizeChanged += OnSizeChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        SizeChanged -= OnSizeChanged;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_overlayVisual != null)
        {
            _overlayVisual.Size = new Vector2((float)e.NewSize.Width, (float)e.NewSize.Height);
        }
    }

    public void Show()
    {
        if (_isOpen) return;
        _isOpen = true;

        Visibility = Visibility.Visible;
        _searchBox?.Focus(FocusState.Programmatic);
        _searchBox?.SelectAll();

        AnimateOpen();
        Opened?.Invoke(this, EventArgs.Empty);
    }

    public void Hide()
    {
        if (!_isOpen) return;
        _isOpen = false;

        AnimateClose();
        Closed?.Invoke(this, EventArgs.Empty);
    }

    public void Toggle()
    {
        if (_isOpen)
            Hide();
        else
            Show();
    }

    private void AnimateOpen()
    {
        if (_compositor == null || _overlayVisual == null || _panelBorder == null) return;

        // Animate overlay fade in
        var overlayAnimation = _compositor.CreateScalarKeyFrameAnimation();
        overlayAnimation.InsertKeyFrame(0f, 0f);
        overlayAnimation.InsertKeyFrame(1f, 0.8f);
        overlayAnimation.Duration = TimeSpan.FromMilliseconds(150);
        overlayAnimation.EasingFunction = _compositor.CreateCubicEase();
        _overlayVisual.StartAnimation("Opacity", overlayAnimation);

        // Animate panel slide down + fade in
        var panelVisual = ElementCompositionPreview.GetElementVisual(_panelBorder);
        panelVisual.Offset = new Vector3(0, -50, 0);
        panelVisual.Opacity = 0f;

        var offsetAnimation = _compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.InsertKeyFrame(0f, new Vector3(0, -50, 0));
        offsetAnimation.InsertKeyFrame(1f, Vector3.Zero);
        offsetAnimation.Duration = TimeSpan.FromMilliseconds(200);
        offsetAnimation.EasingFunction = _compositor.CreateCubicEase();
        panelVisual.StartAnimation("Offset", offsetAnimation);

        var opacityAnimation = _compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.InsertKeyFrame(0f, 0f);
        opacityAnimation.InsertKeyFrame(1f, 1f);
        opacityAnimation.Duration = TimeSpan.FromMilliseconds(200);
        panelVisual.StartAnimation("Opacity", opacityAnimation);
    }

    private void AnimateClose()
    {
        if (_compositor == null || _overlayVisual == null || _panelBorder == null)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        var panelVisual = ElementCompositionPreview.GetElementVisual(_panelBorder);

        var overlayAnimation = _compositor.CreateScalarKeyFrameAnimation();
        overlayAnimation.InsertKeyFrame(0f, _overlayVisual.Opacity);
        overlayAnimation.InsertKeyFrame(1f, 0f);
        overlayAnimation.Duration = TimeSpan.FromMilliseconds(100);
        _overlayVisual.StartAnimation("Opacity", overlayAnimation);

        var offsetAnimation = _compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.InsertKeyFrame(0f, panelVisual.Offset);
        offsetAnimation.InsertKeyFrame(1f, new Vector3(0, -50, 0));
        offsetAnimation.Duration = TimeSpan.FromMilliseconds(150);
        offsetAnimation.EasingFunction = _compositor.CreateCubicEase();
        panelVisual.StartAnimation("Offset", offsetAnimation);

        var opacityAnimation = _compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.InsertKeyFrame(0f, panelVisual.Opacity);
        opacityAnimation.InsertKeyFrame(1f, 0f);
        opacityAnimation.Duration = TimeSpan.FromMilliseconds(150);
        panelVisual.StartAnimation("Opacity", opacityAnimation);

        // Hide after animation
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            Visibility = Visibility.Collapsed;
        };
        timer.Start();
    }

    private async void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestBoxTextChangeReason.UserInput)
        {
            await PerformSearchAsync(sender.Text);
        }
    }

    private async Task PerformSearchAsync(string query)
    {
        if (_appLauncher == null) return;

        // Debounce
        _debounceTimer?.Stop();
        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _debounceTimer.Tick += async (s, e) =>
        {
            _debounceTimer.Stop();
            try
            {
                var results = await _appLauncher.SearchAsync(query, 10);
                if (_resultsList != null)
                {
                    _resultsList.ItemsSource = results;
                    if (results.Any())
                    {
                        _resultsList.SelectedIndex = 0;
                        _resultsList.ScrollIntoView(results.First());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            }
        };
        _debounceTimer.Start();
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is AppInfo app)
        {
            LaunchApp(app);
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            // Try to launch as path
            if (_resultsList?.SelectedItem is AppInfo selectedApp)
            {
                LaunchApp(selectedApp);
            }
        }
    }

    private void OnSearchKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            Hide();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Down)
        {
            if (_resultsList?.Items.Count > 0)
            {
                _resultsList.Focus(FocusState.Programmatic);
                _resultsList.SelectedIndex = 0;
            }
            e.Handled = true;
        }
    }

    private void OnSearchLostFocus(object sender, RoutedEventArgs e)
    {
        // Don't close immediately - allow clicking results
        // The window will close when clicking outside or pressing Escape
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_resultsList?.SelectedItem is AppInfo app)
        {
            _resultsList.ScrollIntoView(app);
        }
    }

    private void OnResultsKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (_resultsList?.SelectedItem is AppInfo app)
            {
                LaunchApp(app);
            }
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            Hide();
            _searchBox?.Focus(FocusState.Programmatic);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Up)
        {
            if (_resultsList?.SelectedIndex == 0)
            {
                _searchBox?.Focus(FocusState.Programmatic);
                _resultsList.SelectedIndex = -1;
            }
            e.Handled = true;
        }
    }

    private async void LaunchApp(AppInfo app)
    {
        if (_appLauncher != null)
        {
            var success = await _appLauncher.LaunchAsync(app);
            if (success)
            {
                AppLaunched?.Invoke(this, app);
                Hide();
            }
        }
    }
}