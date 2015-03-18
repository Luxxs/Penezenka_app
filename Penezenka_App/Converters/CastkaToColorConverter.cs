using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Penezenka_App.Converters
{
    public class CastkaToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            double i = (double)value;
            if (i > 0)
                return new SolidColorBrush(Colors.DarkGreen);
            else if(i < 0)
                return new SolidColorBrush(Colors.Crimson);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            return new NotImplementedException();
        }
    }
}
