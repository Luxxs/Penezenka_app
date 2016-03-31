using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Penezenka_App.Model;

namespace Penezenka_App.Converters
{
    internal class DisabledToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is RecurrenceChain)
            {
                var recChain = (RecurrenceChain) value;
                if (recChain.ID != 0 && recChain.Disabled == true)
                {
                    return "×";
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
