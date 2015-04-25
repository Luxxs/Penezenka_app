using System;
using Windows.UI.Xaml.Data;
using Penezenka_App.Model;

namespace Penezenka_App.Converters
{
    class RecurrenceToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is RecurrenceChain && ((RecurrenceChain)value).ID != 0 && !((RecurrenceChain)value).Disabled)
                return "↺";
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
