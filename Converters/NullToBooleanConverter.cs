// NoorRAC/Converters/NullToBooleanConverter.cs
using System.Globalization;
using System.Windows.Data;

namespace NoorRAC.Converters // Make sure this namespace matches your project structure
{
    public class NullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = (parameter as string)?.ToLower() == "invert";
            bool result = value != null;
            return invert ? !result : result;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}