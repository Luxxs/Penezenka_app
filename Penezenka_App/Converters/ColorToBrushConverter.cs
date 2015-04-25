using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Penezenka_App.OtherClasses;

namespace Penezenka_App.Converters
{
    class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if (value is uint)
                return new SolidColorBrush(MyColors.UIntToColor((uint)value));
            if (value is Color)
                return new SolidColorBrush((Color) value);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            return new NotImplementedException();
        }
    }
}
