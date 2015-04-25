using System;
using Windows.UI.Xaml.Data;

namespace Penezenka_App.Converters
{
    class DateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((DateTimeOffset)value).ToString("d.M");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
