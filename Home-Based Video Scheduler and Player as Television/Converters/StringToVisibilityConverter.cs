using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Home_Based_Video_Scheduler_and_Player_as_Television.Converters
{
    /// <summary>
    /// Returns Visible when the bound string is non-null and non-empty,
    /// Collapsed otherwise. No chaining needed.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
