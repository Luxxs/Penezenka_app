using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Penezenka_App.Converters
{
    public sealed class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (value == null)
                return Visibility.Collapsed;
            if (value is bool)
            {
                bool val = (bool) value;
                if (parameter is bool && (bool) parameter)
                    val = !val;
                return (val) ? Visibility.Visible : Visibility.Collapsed;
            }
            if (value is int)
            {
                var i = (int)value;
                if (i > 0)
                    return Visibility.Collapsed;
                return Visibility.Visible;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            return new NotImplementedException();
        }
    }
}
