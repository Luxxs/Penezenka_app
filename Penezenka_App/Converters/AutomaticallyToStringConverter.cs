using System;
using Windows.UI.Xaml.Data;

namespace Penezenka_App.Converters
{
    internal class AutomaticallyToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool && (bool)value)
                return "A";
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
