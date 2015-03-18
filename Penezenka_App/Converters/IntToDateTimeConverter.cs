using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Penezenka_App.Model;

namespace Penezenka_App.Converters
{
    class IntToDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new DateTimeOffset(Polozka.IntToDateTime((int)value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // pouze převod ze zdroje do cíle
            throw new NotSupportedException();
        }
    }
}
