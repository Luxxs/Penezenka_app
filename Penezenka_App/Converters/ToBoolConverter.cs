using System;
using Windows.UI.Xaml.Data;

namespace Penezenka_App.Converters
{
    class ToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(parameter != null)
                return !System.Convert.ToBoolean(value);

            return System.Convert.ToBoolean(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
