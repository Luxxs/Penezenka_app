using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Penezenka_App.Model;
using Penezenka_App.ViewModel;

namespace Penezenka_App.Converters
{
    class DateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType != typeof(string))
                throw new InvalidOperationException("Cil neni string.");
            return RecordsViewModel.IntToDateTime((int)value).ToString("d.M");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // pouze převod ze zdroje do cíle
            throw new NotSupportedException();
        }
    }
}
