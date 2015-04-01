﻿using System;
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

        public void GetAccounts(bool all=false)
        {
            var stmt = DB.Conn.Prepare("SELECT ID,Title,Balance,Notes FROM Accounts"+((all) ? "" : " WHERE ID<>0"));
            while (stmt.Step() == SQLiteResult.ROW)
            {
                Accounts.Add(new Account
                {
                    ID = (int) stmt.GetInteger(0),
                    Title = stmt.GetText(1),
                    Balance = stmt.GetFloat(2),
                    Notes = stmt.GetText(3)
                });
            }
        }

        public static void InsertAccount(string title, double balance, string notes)
        {
            var stmt = DB.Conn.Prepare("INSERT INTO Accounts (Title,Balance,Notes) VALUES (?,?,?)");
            stmt.Bind(1,title);
            stmt.Bind(2,balance);
            stmt.Bind(3,notes);
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

        public static void DeleteAccount(int id, int newAccountId=-1)
        {
            var stmt = DB.Conn.Prepare("BEGIN TRANSACTION");
            stmt.Step();

            if (newAccountId == -1)
            {
                stmt = DB.Conn.Prepare("DELETE FROM Records WHERE Account=?");
                stmt.Bind(1, id);
                stmt.Step();
            }
            else
            {
                stmt = DB.Conn.Prepare("UPDATE Records SET Account=? WHERE Account=?");
                stmt.Bind(1,newAccountId);
                stmt.Bind(2,id);
                stmt.Step();
            }
            stmt = DB.Conn.Prepare("DELETE FROM Accounts WHERE ID=?");
            stmt.Bind(1,id);
            stmt.Step();

            stmt = DB.Conn.Prepare("COMMIT TRANSACTION");
            stmt.Step();
        }
    }
}