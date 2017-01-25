using Penezenka_App.ViewModel;
using System;
using Windows.UI.Xaml.Data;

namespace Penezenka_App.Converters
{
    class FilterToBalanceStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string culture)
        {
            if(value is RecordsViewModel.Filter) {
                var filter = (RecordsViewModel.Filter)value;
                if(filter.AllAccounts)
                {
                    return "Celkový současný zůstatek:";
                }
                else if(filter.Accounts.Count == 1)
                {
                    return "Současný zůstatek na vybraném účtu:";
                }
                else
                {
                    return "Současný zůstatek na vybraných účtech:";
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            return new NotImplementedException();
        }
    }
}
