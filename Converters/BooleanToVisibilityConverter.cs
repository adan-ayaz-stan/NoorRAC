// NoorRAC/Converters/BooleanToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NoorRAC.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // Optional: Check for parameter to invert (e.g., "invert" or "reverse")
                bool invert = (parameter as string)?.Equals("invert", StringComparison.OrdinalIgnoreCase) ?? false;
                if (invert)
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed; // Default if not a bool
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibilityValue)
            {
                bool invert = (parameter as string)?.Equals("invert", StringComparison.OrdinalIgnoreCase) ?? false;
                bool isVisible = visibilityValue == Visibility.Visible;
                return invert ? !isVisible : isVisible;
            }
            return false;
        }
    }
}