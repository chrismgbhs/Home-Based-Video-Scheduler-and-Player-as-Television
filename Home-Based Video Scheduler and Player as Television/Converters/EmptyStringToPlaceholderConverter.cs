using System;
using System.Globalization;
using System.Windows.Data;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Converters
{
    /// <summary>
    /// Returns a placeholder string when the value is null or empty,
    /// otherwise returns the value itself.
    /// Pass the placeholder as ConverterParameter.
    /// </summary>
    public class EmptyStringToPlaceholderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.IsNullOrEmpty(s))
                return parameter as string ?? "—";
            return s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
