using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Converters
{
    /// <summary>
    /// Converts a CommercialBreak.Offset (TimeSpan) + ShowDurationSeconds (double)
    /// to a left-margin percentage for the timeline bar.
    /// Pass ShowDurationSeconds as ConverterParameter.
    /// Returns a Thickness(leftPercent*trackWidth, 0, 0, 0) via code-behind instead —
    /// here we just return the 0-1 ratio for Canvas.Left binding.
    /// </summary>
    public class OffsetRatioConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan offset && parameter is double totalSeconds && totalSeconds > 0)
                return offset.TotalSeconds / totalSeconds;
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
