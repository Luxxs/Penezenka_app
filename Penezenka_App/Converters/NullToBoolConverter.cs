using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Penezenka_App.ViewModel;

namespace Penezenka_App.Converters
{
    class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value!=null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // pouze převod ze zdroje do cíle
            throw new NotSupportedException();
        }
    }
}
