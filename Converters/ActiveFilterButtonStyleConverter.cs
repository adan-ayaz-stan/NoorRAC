using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NoorRAC.Converters
{
    public class ActiveFilterButtonStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = (bool)value;
            return isActive
                ? Application.Current.FindResource("RadioOptionActiveButton") as Style
                : Application.Current.FindResource("RadioOptionButton") as Style;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}