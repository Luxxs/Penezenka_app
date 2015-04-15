using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Penezenka_App.Model;

namespace Penezenka_App.Converters
{
    class EmptyRowHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string && parameter is RecurrenceChain &&
                string.IsNullOrEmpty((string) value) && ((RecurrenceChain) parameter).ID == 0)
            {
                return 0.0;
            }
            if(value is RecurrenceChain && ((RecurrenceChain) value).ID == 0)
                return 0.0;

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // pouze převod ze zdroje do cíle
            throw new NotSupportedException();
        }
    }
}
