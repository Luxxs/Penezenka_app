using System;
using Windows.UI.Xaml.Data;

namespace Penezenka_App.Converters
{
    class EmptyTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string && string.IsNullOrEmpty(value as string))
                return "Položka bez názvu";
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
