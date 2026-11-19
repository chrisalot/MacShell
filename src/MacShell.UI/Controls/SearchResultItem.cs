using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MacShell.Core.Interfaces;

namespace MacShell.UI.Controls;

/// <summary>
/// A search result item for the launcher.
/// </summary>
public sealed class SearchResultItem : Control
{
    private Image? _iconImage;
    private TextBlock? _nameText;
    private TextBlock? _detailText;
    private Border? _backgroundBorder;

    public SearchResultItem()
    {
        DefaultStyleKey = typeof(SearchResultItem);
    }

    #region IconSource Property

    public ImageSource? IconSource
    {
        get => (ImageSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register(nameof(IconSource), typeof(ImageSource), typeof(SearchResultItem),
            new PropertyMetadata(null, OnIconSourceChanged));

    private static void OnIconSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SearchResultItem item)
        {
            item.UpdateIcon();
        }
    }

    #endregion

    #region IconSize Property

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(SearchResultItem),
            new PropertyMetadata(24.0));

    #endregion

    #region Name Property

    public string Name
    {
        get => (string)GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    public static readonly DependencyProperty NameProperty =
        DependencyProperty.Register(nameof(Name), typeof(string), typeof(SearchResultItem),
            new PropertyMetadata("", OnNameChanged));

    private static void OnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SearchResultItem item && item._nameText != null)
        {
            item._nameText.Text = e.NewValue as string ?? "";
        }
    }

    #endregion

    #region Detail Property

    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public static readonly DependencyProperty DetailProperty =
        DependencyProperty.Register(nameof(Detail), typeof(string), typeof(SearchResultItem),
            new PropertyMetadata("", OnDetailChanged));

    private static void OnDetailChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SearchResultItem item && item._detailText != null)
        {
            var detail = e.NewValue as string ?? "";
            item._detailText.Text = detail;
            item._detailText.Visibility = string.IsNullOrEmpty(detail) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    #endregion

    #region IsSelected Property

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(SearchResultItem),
            new PropertyMetadata(false, OnIsSelectedChanged));

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SearchResultItem item)
        {
            VisualStateManager.GoToState(item, (bool)e.NewValue ? "Selected" : "Normal", true);
        }
    }

    #endregion

    #region HoverBackground Property

    public Brush HoverBackground
    {
        get => (Brush)GetValue(HoverBackgroundProperty);
        set => SetValue(HoverBackgroundProperty, value);
    }

    public static readonly DependencyProperty HoverBackgroundProperty =
        DependencyProperty.Register(nameof(HoverBackground), typeof(Brush), typeof(SearchResultItem),
            new PropertyMetadata(null));

    #endregion

    #region SelectedBackground Property

    public Brush SelectedBackground
    {
        get => (Brush)GetValue(SelectedBackgroundProperty);
        set => SetValue(SelectedBackgroundProperty, value);
    }

    public static readonly DependencyProperty SelectedBackgroundProperty =
        DependencyProperty.Register(nameof(SelectedBackground), typeof(Brush), typeof(SearchResultItem),
            new PropertyMetadata(null));

    #endregion

    #region CornerRadius Property

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(SearchResultItem),
            new PropertyMetadata(new CornerRadius(8)));

    #endregion

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _iconImage = GetTemplateChild("IconImage") as Image;
        _nameText = GetTemplateChild("NameText") as TextBlock;
        _detailText = GetTemplateChild("DetailText") as TextBlock;
        _backgroundBorder = GetTemplateChild("BackgroundBorder") as Border;

        UpdateIcon();
        if (_nameText != null) _nameText.Text = Name;
        if (_detailText != null)
        {
            _detailText.Text = Detail;
            _detailText.Visibility = string.IsNullOrEmpty(Detail) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void UpdateIcon()
    {
        if (_iconImage != null && DataContext is AppInfo app)
        {
            try
            {
                if (!string.IsNullOrEmpty(app.IconPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.UriSource = new Uri(app.IconPath);
                    bitmap.DecodePixelWidth = (int)IconSize;
                    _iconImage.Source = bitmap;
                }
            }
            catch
            {
                _iconImage.Source = null;
            }
        }
    }
}