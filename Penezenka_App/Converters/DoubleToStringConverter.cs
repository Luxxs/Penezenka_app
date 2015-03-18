using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Penezenka_App.Converters
{
    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType != typeof(string))
                throw new InvalidOperationException("Cil neni string.");
            return ((double)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // pouze převod ze zdroje do cíle
            throw new NotSupportedException();
        }
    }
}