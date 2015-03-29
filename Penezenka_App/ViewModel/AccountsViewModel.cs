using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Penezenka_App.Database;
using Penezenka_App.Model;
using SQLitePCL;

namespace Penezenka_App.ViewModel
{
    class AccountsViewModel
    {
        public ObservableCollection<Account> Accounts = new ObservableCollection<Account>();

        public void GetAccounts()
        {
            var stmt = DB.Conn.Prepare("SELECT * FROM Accounts");
            while (stmt.Step() == SQLiteResult.ROW)
            {
                Accounts.Add(new Account()
                {
                    ID = (int)stmt.GetInteger(0),
                    Title = stmt.GetText(1),
                    Notes = stmt.GetText(2)
                });
            }
        }

        public static void InsertAccount(string title, string notes)
        {
            var stmt = DB.Conn.Prepare("INSERT INTO Accounts (Title,Notes) VALUES (?,?)");
            stmt.Bind(1,title);
            stmt.Bind(2,notes);
            stmt.Step();
        }

        public static void UpdateAccount(int id, string title, string notes)
        {
            var stmt = DB.Conn.Prepare("UPDATE Accounts SET Title=?, Notes=? WHERE ID=?");
            stmt.Bind(1,title);
            stmt.Bind(2,notes);
            stmt.Bind(3,id);
            stmt.Step();
        }

        public static void DeleteAccount(int id)
        {
            var stmt = DB.Conn.Prepare("BEGIN TRANSACTION");
            stmt.Step();

            stmt = DB.Conn.Prepare("DELETE FROM Records WHERE Account=?");
            stmt.Bind(1,id);
            stmt.Step();
            stmt = DB.Conn.Prepare("DELETE FROM Accounts WHERE ID=?");
            stmt.Bind(1,id);
            stmt.Step();

            stmt = DB.Conn.Prepare("COMMIT TRANSACTION");
            stmt.Step();
        }
    }
}
