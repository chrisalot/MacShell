using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Numerics;
using Windows.UI;

namespace MacShell.UI.Controls;

/// <summary>
/// A panel with rounded corners and optional drop shadow.
/// Uses Composition API for performant shadows.
/// </summary>
public sealed class RoundedPanel : Panel
{
    private DropShadow? _dropShadow;
    private Compositor? _compositor;
    private SpriteVisual? _shadowVisual;
    private Border? _border;

    public RoundedPanel()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;

        // Create a Border as child for rounded corners visual
        _border = new Border();
        Children.Add(_border);
    }

    #region CornerRadius Property

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(RoundedPanel),
            new PropertyMetadata(new CornerRadius(0), OnCornerRadiusChanged));

    private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RoundedPanel panel)
        {
            panel.UpdateCornerRadius();
        }
    }

    #endregion

    #region Shadow Property

    public Microsoft.UI.Composition.DropShadow? Shadow
    {
        get => (DropShadow?)GetValue(ShadowProperty);
        set => SetValue(ShadowProperty, value);
    }

    public static readonly DependencyProperty ShadowProperty =
        DependencyProperty.Register(nameof(Shadow), typeof(Microsoft.UI.Composition.DropShadow), typeof(RoundedPanel),
            new PropertyMetadata(null, OnShadowChanged));

    private static void OnShadowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RoundedPanel panel)
        {
            panel.UpdateShadow();
        }
    }

    #endregion

    #region ShadowColor Property

    public Color ShadowColor
    {
        get => (Color)GetValue(ShadowColorProperty);
        set => SetValue(ShadowColorProperty, value);
    }

    public static readonly DependencyProperty ShadowColorProperty =
        DependencyProperty.Register(nameof(ShadowColor), typeof(Color), typeof(RoundedPanel),
            new PropertyMetadata(Microsoft.UI.Colors.Black, OnShadowColorChanged));

    private static void OnShadowColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RoundedPanel panel)
        {
            panel.UpdateShadowColor();
        }
    }

    #endregion

    #region ShadowOpacity Property

    public double ShadowOpacity
    {
        get => (double)GetValue(ShadowOpacityProperty);
        set => SetValue(ShadowOpacityProperty, value);
    }

    public static readonly DependencyProperty ShadowOpacityProperty =
        DependencyProperty.Register(nameof(ShadowOpacity), typeof(double), typeof(RoundedPanel),
            new PropertyMetadata(0.25, OnShadowOpacityChanged));

    private static void OnShadowOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RoundedPanel panel)
        {
            panel.UpdateShadowOpacity();
        }
    }

    #endregion

    #region ShadowBlurRadius Property

    public double ShadowBlurRadius
    {
        get => (double)GetValue(ShadowBlurRadiusProperty);
        set => SetValue(ShadowBlurRadiusProperty, value);
    }

    public static readonly DependencyProperty ShadowBlurRadiusProperty =
        DependencyProperty.Register(nameof(ShadowBlurRadius), typeof(double), typeof(RoundedPanel),
            new PropertyMetadata(20.0, OnShadowBlurRadiusChanged));

    private static void OnShadowBlurRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RoundedPanel panel)
        {
            panel.UpdateShadowBlurRadius();
        }
    }

    #endregion

    #region ShadowOffset Property

    public Vector2 ShadowOffset
    {
        get => (Vector2)GetValue(ShadowOffsetProperty);
        set => SetValue(ShadowOffsetProperty, value);
    }

    public static readonly DependencyProperty ShadowOffsetProperty =
        DependencyProperty.Register(nameof(ShadowOffset), typeof(Vector2), typeof(RoundedPanel),
            new PropertyMetadata(new Vector2(0, 4), OnShadowOffsetChanged));

    private static void OnShadowOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RoundedPanel panel)
        {
            panel.UpdateShadowOffset();
        }
    }

    #endregion

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitializeComposition();
        UpdateCornerRadius();
        UpdateShadow();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CleanupComposition();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateShadowVisualSize();
    }

    private void InitializeComposition()
    {
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        _shadowVisual = _compositor.CreateSpriteVisual();
        _shadowVisual.Size = new Vector2((float)ActualWidth, (float)ActualHeight);
        ElementCompositionPreview.SetElementChildVisual(this, _shadowVisual);
    }

    private void CleanupComposition()
    {
        if (_shadowVisual != null)
        {
            ElementCompositionPreview.SetElementChildVisual(this, null);
            _shadowVisual.Dispose();
            _shadowVisual = null;
        }
        _dropShadow?.Dispose();
        _dropShadow = null;
    }

    private void UpdateCornerRadius()
    {
        // Update the internal border's corner radius
        if (_border != null)
        {
            _border.CornerRadius = CornerRadius;
        }

        // For composition shadow, we need to update the mask
        if (_shadowVisual != null && _compositor != null)
        {
            var mask = _compositor.CreateRoundedRectangleGeometry();
            mask.CornerRadius = new Vector2((float)CornerRadius.TopLeft, (float)CornerRadius.TopLeft);
            // The shadow mask would need a more complex setup for per-corner radii
        }
    }

    private void UpdateShadow()
    {
        if (_compositor == null) return;

        if (Shadow != null)
        {
            // Use the provided DropShadow resource
            _dropShadow = Shadow;
        }
        else
        {
            // Create default drop shadow
            _dropShadow = _compositor.CreateDropShadow();
            _dropShadow.Color = ShadowColor;
            _dropShadow.Opacity = (float)ShadowOpacity;
            _dropShadow.BlurRadius = (float)ShadowBlurRadius;
            _dropShadow.Offset = new Vector3(ShadowOffset.X, ShadowOffset.Y, 0);
        }

        // Create mask from the internal border's visual
        if (_border != null)
        {
            var borderVisual = ElementCompositionPreview.GetElementVisual(_border);
            // Mask expects a CompositionBrush - use a simple approach with rounded rectangle geometry
            // For now, skip mask to avoid complexity - shadow will be rectangular
            // var maskBrush = _compositor.CreateMaskBrush();
            // maskBrush.Source = _compositor.CreateVisualSurfaceBrush(borderVisual);
            // _dropShadow.Mask = maskBrush;
        }

        if (_shadowVisual != null)
        {
            _shadowVisual.Shadow = _dropShadow;
        }
    }

    private void UpdateShadowVisualSize()
    {
        if (_shadowVisual != null)
        {
            _shadowVisual.Size = new Vector2((float)ActualWidth, (float)ActualHeight);
        }
    }

    private void UpdateShadowColor()
    {
        if (_dropShadow != null)
        {
            _dropShadow.Color = ShadowColor;
        }
    }

    private void UpdateShadowOpacity()
    {
        if (_dropShadow != null)
        {
            _dropShadow.Opacity = (float)ShadowOpacity;
        }
    }

    private void UpdateShadowBlurRadius()
    {
        if (_dropShadow != null)
        {
            _dropShadow.BlurRadius = (float)ShadowBlurRadius;
        }
    }

    private void UpdateShadowOffset()
    {
        if (_dropShadow != null)
        {
            _dropShadow.Offset = new Vector3(ShadowOffset.X, ShadowOffset.Y, 0);
        }
    }
}