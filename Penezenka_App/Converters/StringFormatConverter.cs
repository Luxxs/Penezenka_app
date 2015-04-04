using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Penezenka_App.Converters
{
    class StringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return string.Format(CultureInfo.CurrentUICulture, parameter as string, value);
    }  

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return null;
    }
}
}
