using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using MacShell.Core.Models;

namespace MacShell.UI.Converters;

/// <summary>
/// Converts boolean to Visibility (true = Visible, false = Collapsed).
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            var invert = parameter?.ToString()?.Equals("invert", StringComparison.OrdinalIgnoreCase) == true;
            return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            var invert = parameter?.ToString()?.Equals("invert", StringComparison.OrdinalIgnoreCase) == true;
            return (visibility == Visibility.Visible) ^ invert;
        }
        return false;
    }
}

/// <summary>
/// Converts boolean to Visibility (true = Visible, false = Hidden).
/// </summary>
public sealed class BoolToVisibilityHiddenConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            var invert = parameter?.ToString()?.Equals("invert", StringComparison.OrdinalIgnoreCase) == true;
            return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            var invert = parameter?.ToString()?.Equals("invert", StringComparison.OrdinalIgnoreCase) == true;
            return (visibility == Visibility.Visible) ^ invert;
        }
        return false;
    }
}

/// <summary>
/// Converts running state to running indicator visibility.
/// </summary>
public sealed class RunningStateToIndicatorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isRunning)
        {
            return isRunning ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility.Visible;
    }
}

/// <summary>
/// Converts theme enum to brush.
/// </summary>
public sealed class ThemeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ElementTheme theme)
        {
            var resourceKey = parameter?.ToString() ?? "MacShellBackgroundBrush";
            return Application.Current.Resources.TryGetValue(resourceKey, out var resource) ? resource : Application.Current.Resources["MacShellBackgroundBrush"];
        }
        return Application.Current.Resources["MacShellBackgroundBrush"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a value to its inverse.
/// </summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}

/// <summary>
/// Converts null to boolean (true if not null).
/// </summary>
public sealed class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var invert = parameter?.ToString()?.Equals("invert", StringComparison.OrdinalIgnoreCase) == true;
        return invert ? value == null : value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts string to uppercase.
/// </summary>
public sealed class StringToUpperConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString()?.ToUpperInvariant() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Formats a DateTime as time string.
/// </summary>
public sealed class DateTimeToTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dateTime)
        {
            var format = parameter?.ToString() ?? "HH:mm";
            return dateTime.ToString(format);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Formats a battery percentage to string.
/// </summary>
public sealed class BatteryToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int percentage)
        {
            return $"{percentage}%";
        }
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts battery status to icon name.
/// </summary>
public sealed class BatteryStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is MacShell.Core.Interfaces.BatteryStatus status)
        {
            return status switch
            {
                MacShell.Core.Interfaces.BatteryStatus.Charging => "BatteryCharging",
                MacShell.Core.Interfaces.BatteryStatus.Full => "BatteryFull",
                MacShell.Core.Interfaces.BatteryStatus.Discharging => "BatteryDischarging",
                MacShell.Core.Interfaces.BatteryStatus.NotPresent => "BatteryNotPresent",
                _ => "BatteryUnknown"
            };
        }
        return "BatteryUnknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts network connection type to icon name.
/// </summary>
public sealed class NetworkTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is MacShell.Core.Interfaces.NetworkConnectionType type)
        {
            return type switch
            {
                MacShell.Core.Interfaces.NetworkConnectionType.Wifi => "Wifi",
                MacShell.Core.Interfaces.NetworkConnectionType.Ethernet => "Ethernet",
                MacShell.Core.Interfaces.NetworkConnectionType.Cellular => "Cellular",
                MacShell.Core.Interfaces.NetworkConnectionType.VPN => "VPN",
                _ => "NetworkUnknown"
            };
        }
        return "NetworkUnknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts signal strength to bars (0-4).
/// </summary>
public sealed class SignalStrengthToBarsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int strength)
        {
            return strength switch
            {
                >= 80 => 4,
                >= 60 => 3,
                >= 40 => 2,
                >= 20 => 1,
                _ => 0
            };
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts double to string with formatting.
/// </summary>
public sealed class DoubleToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double d)
        {
            var format = parameter?.ToString() ?? "F1";
            return d.ToString(format);
        }
        return "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (double.TryParse(value?.ToString(), out var result))
        {
            return result;
        }
        return 0.0;
    }
}

/// <summary>
/// Multi-converter for checking if any value is true.
/// </summary>
public sealed class OrMultiConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is object[] values)
        {
            foreach (var v in values)
            {
                if (v is bool b && b) return true;
            }
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Multi-converter for checking if all values are true.
/// </summary>
public sealed class AndMultiConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is object[] values)
        {
            foreach (var v in values)
            {
                if (v is bool b && !b) return false;
            }
            return true;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a DockItem to its display name.
/// </summary>
public sealed class DockItemToNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DockItem item)
        {
            return item.Name;
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a DockItem to its icon path.
/// </summary>
public sealed class DockItemToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DockItem item)
        {
            return item.IconPath;
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}