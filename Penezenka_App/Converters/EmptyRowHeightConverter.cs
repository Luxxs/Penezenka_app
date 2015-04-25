using System;
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
            throw new NotSupportedException();
        }
    }
}
