using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System.Numerics;
using MacShell.Core.Models;

namespace MacShell.UI.Controls;

/// <summary>
/// A dock panel with macOS-style magnification effect on hover.
/// Uses Composition API for smooth 60fps animations.
/// </summary>
public sealed class DockPanel : Control
{
    private ItemsRepeater? _itemsRepeater;
    private Compositor? _compositor;
    private Dictionary<string, DockItemVisualState> _itemStates = new();
    private DockItemControl? _hoveredItem;
    private double _magnificationFactor = 1.4;
    private double _magnificationRadius = 80;
    private bool _isAnimating;

    public DockPanel()
    {
        DefaultStyleKey = typeof(DockPanel);
    }

    #region ItemsSource Property

    public IEnumerable<DockItem> ItemsSource
    {
        get => (IEnumerable<DockItem>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<DockItem>), typeof(DockPanel),
            new PropertyMetadata(null, OnItemsSourceChanged));

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockPanel panel)
        {
            panel.OnItemsSourceChanged((IEnumerable<DockItem>?)e.OldValue, (IEnumerable<DockItem>?)e.NewValue);
        }
    }

    #endregion

    #region ItemTemplate Property

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(DockPanel),
            new PropertyMetadata(null));

    #endregion

    #region MagnificationFactor Property

    public double MagnificationFactor
    {
        get => (double)GetValue(MagnificationFactorProperty);
        set => SetValue(MagnificationFactorProperty, value);
    }

    public static readonly DependencyProperty MagnificationFactorProperty =
        DependencyProperty.Register(nameof(MagnificationFactor), typeof(double), typeof(DockPanel),
            new PropertyMetadata(1.4));

    #endregion

    #region MagnificationRadius Property

    public double MagnificationRadius
    {
        get => (double)GetValue(MagnificationRadiusProperty);
        set => SetValue(MagnificationRadiusProperty, value);
    }

    public static readonly DependencyProperty MagnificationRadiusProperty =
        DependencyProperty.Register(nameof(MagnificationRadius), typeof(double), typeof(DockPanel),
            new PropertyMetadata(80.0));

    #endregion

    #region ItemSpacing Property

    public double ItemSpacing
    {
        get => (double)GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    public static readonly DependencyProperty ItemSpacingProperty =
        DependencyProperty.Register(nameof(ItemSpacing), typeof(double), typeof(DockPanel),
            new PropertyMetadata(8.0, OnItemSpacingChanged));

    private static void OnItemSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockPanel panel && panel._itemsRepeater != null)
        {
            if (panel._itemsRepeater.Layout is StackLayout stackLayout)
            {
                stackLayout.Spacing = (double)e.NewValue;
            }
        }
    }

    #endregion

    #region ItemClick Event

    public event EventHandler<DockItem>? ItemClick;
    public event EventHandler<DockItem>? ItemRightClick;
    public event EventHandler<DockItem>? ItemDragStart;

    #endregion

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _itemsRepeater = GetTemplateChild("ItemsRepeater") as ItemsRepeater;
        if (_itemsRepeater != null)
        {
            _itemsRepeater.Layout = new StackLayout
            {
                Orientation = Orientation.Horizontal,
                Spacing = ItemSpacing
            };
        }

        InitializeComposition();
    }

    private void InitializeComposition()
    {
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
    }

    private void OnItemsSourceChanged(IEnumerable<DockItem>? oldValue, IEnumerable<DockItem>? newValue)
    {
        if (oldValue is System.Collections.Specialized.INotifyCollectionChanged oldObservable)
        {
            oldObservable.CollectionChanged -= OnCollectionChanged;
        }

        if (newValue is System.Collections.Specialized.INotifyCollectionChanged newObservable)
        {
            newObservable.CollectionChanged += OnCollectionChanged;
        }

        // Update ItemsRepeater
        if (_itemsRepeater != null)
        {
            _itemsRepeater.ItemsSource = newValue;
        }

        // Clean up old item states
        if (oldValue != null)
        {
            var oldItems = oldValue.ToList();
            var newItems = newValue?.ToList() ?? new List<DockItem>();
            var removedItems = oldItems.Where(i => !newItems.Contains(i)).ToList();
            foreach (var item in removedItems)
            {
                _itemStates.Remove(item.Id);
            }
        }
    }

    private void OnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Collection changed - ItemsRepeater handles UI updates automatically
    }

    /// <summary>
    /// Called by DockItemControl when pointer enters.
    /// </summary>
    internal void OnItemPointerEntered(DockItemControl item)
    {
        _hoveredItem = item;
        UpdateMagnification(item);
    }

    /// <summary>
    /// Called by DockItemControl when pointer exits.
    /// </summary>
    internal void OnItemPointerExited(DockItemControl item)
    {
        if (_hoveredItem == item)
        {
            _hoveredItem = null;
            ResetMagnification();
        }
    }

    /// <summary>
    /// Called by DockItemControl when clicked.
    /// </summary>
    internal void OnItemClicked(DockItemControl item)
    {
        ItemClick?.Invoke(this, item.DataContext as DockItem);
    }

    /// <summary>
    /// Called by DockItemControl when right-clicked.
    /// </summary>
    internal void OnItemRightClicked(DockItemControl item)
    {
        ItemRightClick?.Invoke(this, item.DataContext as DockItem);
    }

    private void UpdateMagnification(DockItemControl hoveredItem)
    {
        if (_itemsRepeater == null || _compositor == null) return;

        var items = _itemsRepeater.GetElements().OfType<DockItemControl>().ToList();
        var hoveredIndex = items.IndexOf(hoveredItem);
        if (hoveredIndex < 0) return;

        var centerX = GetItemCenterX(hoveredItem);

        foreach (var item in items)
        {
            var itemCenterX = GetItemCenterX(item);
            var distance = Math.Abs(itemCenterX - centerX);

            double scale = 1.0;
            if (distance < MagnificationRadius)
            {
                // Smooth falloff using cosine interpolation
                var t = distance / MagnificationRadius;
                var falloff = (Math.Cos(t * Math.PI) + 1) / 2; // 1 at center, 0 at radius
                scale = 1.0 + (MagnificationFactor - 1.0) * falloff;
            }

            AnimateItemScale(item, scale);
        }
    }

    private void ResetMagnification()
    {
        if (_itemsRepeater == null) return;

        var items = _itemsRepeater.GetElements().OfType<DockItemControl>().ToList();
        foreach (var item in items)
        {
            AnimateItemScale(item, 1.0);
        }
    }

    private void AnimateItemScale(DockItemControl item, double targetScale)
    {
        var visual = ElementCompositionPreview.GetElementVisual(item);
        var currentScale = visual.Scale.X;

        if (Math.Abs(currentScale - targetScale) < 0.01) return;

        var animation = _compositor!.CreateScalarKeyFrameAnimation();
        animation.InsertKeyFrame(0f, (float)currentScale);
        animation.InsertKeyFrame(1f, (float)targetScale);
        animation.Duration = TimeSpan.FromMilliseconds(150);
        animation.IterationBehavior = AnimationIterationBehavior.Count;
        animation.IterationCount = 1;

        // Use spring-like easing
        var easing = _compositor.CreateSpringVector3NaturalMotion();
        // Note: ScalarKeyFrameAnimation doesn't support spring easing directly
        // We'll use a cubic ease out for smooth deceleration
        var cubicEase = _compositor.CreateCubicEase();
        cubicEase.ControlPoint1 = new Vector2(0.25f, 0.1f);
        cubicEase.ControlPoint2 = new Vector2(0.25f, 1f);
        animation.EasingFunction = cubicEase;

        visual.StartAnimation("Scale.X", animation);
        visual.StartAnimation("Scale.Y", animation);

        // Also animate Z to bring magnified items to front
        var zAnimation = _compositor.CreateScalarKeyFrameAnimation();
        zAnimation.InsertKeyFrame(0f, visual.Offset.Z);
        zAnimation.InsertKeyFrame(1f, (float)(targetScale * 10));
        zAnimation.Duration = TimeSpan.FromMilliseconds(150);
        visual.StartAnimation("Offset.Z", zAnimation);
    }

    private double GetItemCenterX(DockItemControl item)
    {
        var transform = item.TransformToVisual(this);
        var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
        return point.X + item.ActualWidth / 2;
    }
}

/// <summary>
/// Visual state for a dock item during magnification.
/// </summary>
internal sealed class DockItemVisualState
{
    public double BaseScale { get; set; } = 1.0;
    public double TargetScale { get; set; } = 1.0;
    public bool IsAnimating { get; set; }
}