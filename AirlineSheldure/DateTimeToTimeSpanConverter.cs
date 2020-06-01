using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace AirlineSheldure
{
	class DateTimeToTimeSpanConverter : IValueConverter
	{
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
            return DateTime.Now.Date + (TimeSpan)value;
            //return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //return DependencyProperty.UnsetValue;
            return ((DateTime)value).TimeOfDay;
        }
    }
}
