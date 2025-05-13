using System;
using System.Globalization;
using System.Windows.Data; // Required for IValueConverter
using System.Windows;      // Required for DependencyProperty.UnsetValue

namespace NoorRAC.Converters // Make sure this namespace matches your project structure
{
    [ValueConversion(typeof(bool), typeof(bool))] // Optional: Helps tools understand the conversion
    public class InverseBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to its inverse.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The type of the binding target property (should be bool).</param>
        /// <param name="parameter">An optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter (not used).</param>
        /// <returns>The inverse boolean value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            // Return false or UnsetValue if the input is not a boolean
            return false; // Or DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// Converts the inverse boolean value back to the original boolean value.
        /// </summary>
        /// <param name="value">The inverse boolean value to convert back.</param>
        /// <param name="targetType">The type of the binding source property (should be bool).</param>
        /// <param name="parameter">An optional parameter (not used).</param>
        /// <param name="culture">The culture to use in the converter (not used).</param>
        /// <returns>The original boolean value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Same logic for converting back
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return false; // Or DependencyProperty.UnsetValue;
        }
    }
}