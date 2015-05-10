using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Penezenka_App.Database;
using Penezenka_App.Model;
using SQLitePCL;

namespace Penezenka_App.ViewModel
{
    class AccountsViewModel
    {
        public ObservableCollection<Account> Accounts = new ObservableCollection<Account>();

        public void GetAccounts(bool all=false, int exceptId=-1)
        {
            var stmt =
                DB.Query("SELECT ID,Title,Notes FROM Accounts" + ((all) ? " WHERE ID<>?" : " WHERE ID<>0 AND ID<>?"),
                    exceptId);
            while (stmt.Step() == SQLiteResult.ROW)
            {
                Accounts.Add(GetAccountFromStatement(stmt));
            }
        }

        public static Account GetAccountByID(int id)
        {
            var stmt = DB.Query("SELECT ID,Title,Notes FROM Accounts WHERE ID=?", id);
            stmt.Step();
            return GetAccountFromStatement(stmt);
        }

        private static Account GetAccountFromStatement(ISQLiteStatement stmt)
        {
            return new Account
            {
                ID = (int) stmt.GetInteger(0),
                Title = stmt.GetText(1),
                Notes = stmt.GetText(2)
            };
        }


        public static void InsertAccount(string title, double startBalance, string notes)
        {
            DB.QueryAndStep("INSERT INTO Accounts (Title,Notes) VALUES (?,?)", title, notes);
            if (startBalance != 0)
            {
                int accountId = (int) DB.Conn.LastInsertRowId();
                RecordsViewModel.InsertRecord(accountId, DateTimeOffset.Now, "Počáteční vklad", startBalance, "Na účet: "+title,
                    new List<Tag>(), null, 0);
            }
        }

        public static void UpdateAccount(int id, string title, string notes)
        {
            DB.QueryAndStep("UPDATE Accounts SET Title=?, Notes=? WHERE ID=?", title, notes, id);
        }

        public void DeleteAccount(int id, int newAccountId=-1)
        {
            DB.QueryAndStep("BEGIN TRANSACTION");
            if (newAccountId == -1)
            {
                DB.QueryAndStep("DELETE FROM Records WHERE Account=?", id);
            }
            else
            {
                DB.QueryAndStep("UPDATE Records SET Account=? WHERE Account=?", newAccountId, id);
            }
            DB.QueryAndStep("DELETE FROM Accounts WHERE ID=?", id);
            DB.QueryAndStep("COMMIT TRANSACTION");
            Accounts.Remove(Accounts.First(x => x.ID == id));
        }

        public static int GetNumOfRecordsInAccount(int accountId)
        {
            try
            {
                var stmt = DB.Query("SELECT count(*) FROM Records WHERE Account=?", accountId);
                stmt.Step();
                return (int)stmt.GetInteger(0);
            }
            catch (SQLiteException)
            {
                return 0;
            }
        }
    }
}
