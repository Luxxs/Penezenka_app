using System;
using System.Globalization;
using Windows.UI.Xaml.Data;

namespace Penezenka_App.Converters
{
    class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return string.Format(CultureInfo.CurrentUICulture, parameter as string, value);
        }  

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return double.Parse((string)value, CultureInfo.CurrentUICulture);
        }
    }
}
