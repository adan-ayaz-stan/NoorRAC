// NoorRAC/Converters/ErrorsToStringConverter.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace NoorRAC.Converters
{
    public class ErrorsToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<string> errors)
            {
                return string.Join(Environment.NewLine, errors);
            }
            // If your Errors[PropertyName] returns IEnumerable<ValidationResult>
            // if (value is IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> validationResults)
            // {
            //    return string.Join(Environment.NewLine, validationResults.Select(vr => vr.ErrorMessage));
            // }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}