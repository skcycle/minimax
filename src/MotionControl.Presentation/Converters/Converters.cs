using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MotionControl.Presentation.Converters;

/// <summary>
/// 布尔转颜色转换器 - 用于状态显示
/// </summary>
public class BoolToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var colors = parameter?.ToString()?.Split('|') ?? new[] { "#107C10", "#E81123" };
            var trueColor = colors.Length > 0 ? colors[0] : "#107C10";
            var falseColor = colors.Length > 1 ? colors[1] : "#E81123";

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(boolValue ? trueColor : falseColor));
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 状态转颜色转换器
/// </summary>
public class StateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var state = value?.ToString() ?? "";
        return state switch
        {
            "Moving" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4")),
            "Jogging" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00B7C3")),
            "Homing" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8C00")),
            "Stopping" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8764B8")),
            "Alarm" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E81123")),
            "Enabled" or "Standstill" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#107C10")),
            "Disabled" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C5C5C")),
            _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C5C5C"))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 布尔转文本转换器
/// </summary>
public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var texts = parameter?.ToString()?.Split('|') ?? new[] { "Yes", "No" };
            return boolValue ? texts[0] : (texts.Length > 1 ? texts[1] : "No");
        }
        return "N/A";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 位置格式化转换器
/// </summary>
public class PositionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double pos)
        {
            return pos.ToString("F3");
        }
        return "0.000";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && double.TryParse(str, out var result))
        {
            return result;
        }
        return 0.0;
    }
}

/// <summary>
/// 连接状态转颜色
/// </summary>
public class ConnectionStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#107C10"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E81123"));
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C5C5C"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
