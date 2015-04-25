using System.Collections.ObjectModel;
using Penezenka_App.Model;

namespace Penezenka_App.SampleData
{
    class AccountsModel
    {
        public ObservableCollection<Account> Accounts
        {
            get
            {
                return new ObservableCollection<Account> {
                    new Account {ID=1, Title = "hotovost", Notes="dsfdsf"},
                    new Account {ID=1, Title = "účet - spořka", Notes="kksdfg dfb sdoú¨p"}
                };
            }
        }
    }
}
