using System;
using Windows.UI.Xaml.Data;

namespace Penezenka_App.Converters
{
    class BoolToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (value is bool && ((bool) value) == false)
                return 0;
            return "Auto";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            return new NotImplementedException();
        }
    }
}
