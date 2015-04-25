using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Penezenka_App.OtherClasses;

namespace Penezenka_App.Converters
{
    class ColorToMyColorName : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            uint argb = 0;
            if (value is Color)
                argb = MyColors.ColorToUInt((Color) value);
            else if (value is uint)
                argb = (uint) value;
            int index = Array.IndexOf(MyColors.UIntColors, argb);
            if (index != -1)
                return MyColors.ColorNames[index];
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            return new NotImplementedException();
        }
    }
}
