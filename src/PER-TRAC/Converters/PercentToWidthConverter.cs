using System.Globalization;
using System.Windows.Data;

namespace PerTrac.Converters;

public class PercentToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2
            && values[0] is double percent
            && values[1] is double maxWidth)
        {
            return Math.Max(0, Math.Min(maxWidth, maxWidth * percent / 100.0));
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class TemperatureToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double temp)
        {
            if (temp >= 90) return "#FFFF4444";
            if (temp >= 75) return "#FFFF8800";
            if (temp >= 60) return "#FFFFCC00";
            return "#FF00CC66";
        }
        return "#FF00CC66";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class UsageToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double usage)
        {
            if (usage >= 90) return "#FFFF4444";
            if (usage >= 75) return "#FFFF8800";
            if (usage >= 50) return "#FF00B4FF";
            return "#FF00CC66";
        }
        return "#FF00CC66";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
