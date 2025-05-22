// NoorRAC/Converters/NullToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NoorRAC.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Default: Visible if NOT null, Collapsed if null
            Visibility notNullVisibility = Visibility.Visible;
            Visibility nullVisibility = Visibility.Collapsed;

            if (parameter is string paramString)
            {
                if (paramString.Equals("Hidden", StringComparison.OrdinalIgnoreCase))
                {
                    nullVisibility = Visibility.Hidden;
                }
                else if (paramString.Equals("Invert", StringComparison.OrdinalIgnoreCase))
                {
                    // Inverted: Visible if IS null, Collapsed if NOT null
                    notNullVisibility = Visibility.Collapsed;
                    nullVisibility = Visibility.Visible;
                }
                else if (paramString.Equals("InvertHidden", StringComparison.OrdinalIgnoreCase))
                {
                    notNullVisibility = Visibility.Hidden;
                    nullVisibility = Visibility.Visible;
                }
            }

            return value != null ? notNullVisibility : nullVisibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Not typically needed for visibility
        }
    }
}