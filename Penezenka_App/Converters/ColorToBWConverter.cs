using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Penezenka_App.OtherClasses;

namespace Penezenka_App.Converters
{
    class ColorToBWConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            Color color;

            if (value is uint)
                color = MyColors.UIntToColor((uint)value);
            else if (value is SolidColorBrush)
                color = ((SolidColorBrush) value).Color;
            else
                color = (Color) value;

            if((color.R+color.G+color.B)/3 > 127)
                return new SolidColorBrush(Colors.Black);
            else
                return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            return new NotImplementedException();
        }
    }
}
