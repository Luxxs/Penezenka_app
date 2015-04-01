using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Penezenka_App.Database;
using Penezenka_App.Model;
using SQLitePCL;

namespace Penezenka_App.ViewModel
{
    class RecordsViewModel
    {
        public ObservableCollection<Record> Records { get; set; }
        
        public void GetMonth(int rok, int mesic)
        {
            ISQLiteStatement stmt = DB.Conn.Prepare(@"SELECT ID, Date, Title, Amount, Notes, Account, RecurrenceChain, Automatically FROM Records WHERE Date>? AND Date<?");
            stmt.Bind(1, rok*10000+mesic*100);
            stmt.Bind(2, rok*10000+(mesic+1)*100);
            ObservableCollection<Record> records = new ObservableCollection<Record>();
            while(stmt.Step() == SQLiteResult.ROW)
            {
                Record record = getOtherItemsIntoRecord((int) stmt.GetInteger(0), (int)stmt.GetInteger(6), (int) stmt.GetInteger(5));
                record.ID = (int) stmt.GetInteger(0);
                record.Date = IntToDateTime((int) stmt.GetInteger(1));
                record.Title = stmt.GetText(2);
                record.Amount = stmt.GetFloat(3);
                record.Notes = stmt.GetText(4);
                record.Automatically = Convert.ToBoolean(stmt.GetInteger(6));

                records.Add(record);
            }
            Records = records;
        }

        private Record getOtherItemsIntoRecord(int recordId, int recurrenceChainId, int accountId)
        {
            Record record = new Record();
            ISQLiteStatement tagStmt = DB.Conn.Prepare("SELECT ID, Title, Color, Notes FROM Tags LEFT JOIN RecordsTags ON Tag_ID=ID WHERE Record_ID=?");
            tagStmt.Bind(1,recordId);
            record.Tags = new List<Tag>();
            while (tagStmt.Step() == SQLiteResult.ROW)
            {
                record.Tags.Add(new Tag((int)tagStmt.GetInteger(0), tagStmt.GetText(1), (uint)tagStmt.GetInteger(2), tagStmt.GetText(3)));
            }

            if (recurrenceChainId != 0)
            {
                var recStmt = DB.Conn.Prepare("SELECT Type, Value FROM RecurrenceChains WHERE ID=? AND Disabled<>1");
                recStmt.Bind(1,recurrenceChainId);
                recStmt.Step();
                record.RecurrenceChain = new RecurrenceChain
                {
                    ID = recurrenceChainId,
                    Type = recStmt.GetText(0),
                    Value = (int) recStmt.GetInteger(1)
                };
            } else {
                record.RecurrenceChain = new RecurrenceChain{ID = 0, Type = null, Value = 0};
            }

            if (accountId != 0)
            {
                var accStmt = DB.Conn.Prepare("SELECT Title, Notes FROM Accounts WHERE ID=?");
                accStmt.Bind(1,accountId);
                accStmt.Step();
                record.Account = new Account(){
                                ID=accountId,
                                Title=accStmt.GetText(0),
                                Notes=accStmt.GetText(1)};
            }
            else
            {
                record.Account = new Account(){ID=0,Title="<žádný>",Notes=null};
            }
            return record;
        }

        public static int GetMinYear()
        {
            ISQLiteStatement stmt = DB.Conn.Prepare("SELECT min(Date) FROM Records");
            stmt.Step();
            return (int)stmt.GetInteger(0)/10000;
        }
        public static int GetMaxYear()
        {
            ISQLiteStatement stmt = DB.Conn.Prepare("SELECT min(Date) FROM Records");
            stmt.Step();
            return (int)stmt.GetInteger(0)/10000;
        }

        public static double GetBalance(List<int> accountIds=null)
        {
            ISQLiteStatement stmt;
            if (accountIds != null)
            {
                string idsString = accountIds[0].ToString();
                for (int i = 1; i < accountIds.Count; i++)
                    idsString += "," + accountIds[i];
                stmt = DB.Conn.Prepare("SELECT sum(Amount) FROM Records WHERE Account IN (" + idsString + ")");
            }
            else
            {
                stmt = DB.Conn.Prepare("SELECT sum(Amount) FROM Records");
            }
            stmt.Step();
            try
            {
                return stmt.GetFloat(0);
            }
            catch (SQLiteException)
            {
                return 0;
            }
            
        }



        /* INSERT, UPDATE, DELETE */
        public static void InsertRecord(int accountId, DateTimeOffset date, string name, double amount, string notes,
            List<Tag> tags, string recurrenceType, int recurrenceValue)
        {
            ISQLiteStatement stmt;
            int recurrenceChainID = 0;
            if (recurrenceType != null)
            {
                stmt = DB.Conn.Prepare("INSERT INTO RecurrenceChains (Type,Value,Disabled) VALUES (?,?,0)");
                stmt.Bind(1,recurrenceType);
                stmt.Bind(2,recurrenceValue);
                stmt.Step();
                stmt.Reset();
                recurrenceChainID = (int)DB.Conn.LastInsertRowId();
            }

            stmt = DB.Conn.Prepare("PRAGMA foreign_keys=OFF");
            stmt.Step();
            stmt = DB.Conn.Prepare("INSERT INTO Records (Date,Title,Amount,Notes,Account,RecurrenceChain) VALUES (?,?,?,?,?,?)");
            stmt.Bind(1, DateTimeToInt(date));
            stmt.Bind(2, name);
            stmt.Bind(3, amount);
            stmt.Bind(4, notes);
            stmt.Bind(5, accountId);
            stmt.Bind(6, recurrenceChainID);
            stmt.Step();
            stmt.Reset();
            //pozor na transakce
            int recordID = (int)DB.Conn.LastInsertRowId();
            stmt = DB.Conn.Prepare("PRAGMA foreign_keys=ON");
            stmt.Step();

            foreach (var tag in tags)
            {
                stmt = DB.Conn.Prepare("INSERT INTO RecordsTags (Record_ID, Tag_ID) VALUES (?,?)");
                stmt.Bind(1,recordID);
                stmt.Bind(2,tag.ID);
                stmt.Step();
            }
        }
        public static void UpdateRecord(int recordId, int accountId, DateTimeOffset date, string name, double amount, string notes,
            List<Tag> tags, int recurrenceChainId, string recurrenceType, int recurrenceValue)
        {
            ISQLiteStatement stmt;
            stmt = DB.Conn.Prepare("SELECT Amount FROM Records WHERE ID=?");
            stmt.Bind(1,recordId);
            stmt.Step();

            if(recurrenceChainId != 0) {
                if (recurrenceType == null)
                {
                    stmt = DB.Conn.Prepare(("UPDATE RecurrenceChains SET Disabled=1 WHERE ID=?"));
                    stmt.Bind(1, recurrenceChainId);
                    stmt.Step();
                }
                else
                {
                    stmt = DB.Conn.Prepare(("UPDATE RecurrenceChains SET Type=?, Value=?, Disabled=0 WHERE ID=?"));
                    stmt.Bind(1, recurrenceType);
                    stmt.Bind(2, recurrenceValue);
                    stmt.Bind(3, recurrenceChainId);
                    stmt.Step();
                }
            }
            else if(recurrenceType!=null) {
                stmt = DB.Conn.Prepare("INSERT INTO RecurrenceChains (Type,Value,Disabled) VALUES (?,?,0)");
                stmt.Bind(1,recurrenceType);
                stmt.Bind(2,recurrenceValue);
                stmt.Step();
                stmt.Reset();
                recurrenceChainId = (int)DB.Conn.LastInsertRowId();
            }
            
            stmt = DB.Conn.Prepare("PRAGMA foreign_keys=OFF");
            stmt.Step();
            stmt = DB.Conn.Prepare("UPDATE Records SET Date=?, Title=?, Amount=?, Notes=?, Account=?, RecurrenceChain=? WHERE ID=?");
            stmt.Bind(1, DateTimeToInt(date));
            stmt.Bind(2, name);
            stmt.Bind(3, amount);
            stmt.Bind(4, notes);
            stmt.Bind(5, accountId);
            stmt.Bind(6, recurrenceChainId);
            stmt.Bind(7, recordId);
            stmt.Step();
            stmt = DB.Conn.Prepare("PRAGMA foreign_keys=ON");
            stmt.Step();

            //dalo by se vyhledávat, která přiřazení štítků k záznamům se mají odstranit, ale zde to asi nemá smysl.
            stmt = DB.Conn.Prepare("DELETE FROM RecordsTags WHERE Record_ID=?");
            stmt.Bind(1,recordId);
            stmt.Step();
            foreach (var tag in tags)
            {
                stmt = DB.Conn.Prepare("INSERT INTO RecordsTags (Record_ID, Tag_ID) VALUES (?,?)");
                stmt.Bind(1,recordId);
                stmt.Bind(2,tag.ID);
                stmt.Step();
            }
        }
        public static void DeleteRecord(int recordId, int recurrenceChainId)
        {
            var stmt = DB.Conn.Prepare("SELECT count(*) FROM Records WHERE RecurrenceChain=?");
            stmt.Bind(1, recurrenceChainId);
            stmt.Step();
            int recordsWithRecurrcenceCount = (int) stmt.GetInteger(0);

            stmt = DB.Conn.Prepare("DELETE FROM RecordsTags WHERE Record_ID=?");
            stmt.Bind(1, recordId);
            stmt.Step();
            
            stmt = DB.Conn.Prepare("DELETE FROM Records WHERE ID=?");
            stmt.Bind(1, recordId);
            stmt.Step();

            if (recurrenceChainId != 0 && recordsWithRecurrcenceCount == 1)
            {
                stmt = DB.Conn.Prepare("DELETE FROM RecurrenceChains WHERE ID=?");
                stmt.Bind(1, recurrenceChainId);
                stmt.Step();
            }
        }


        public static DateTime IntToDateTime(int datum)
        {
            int rok = datum/10000;
            int mesic = datum/100 - rok*100;
            int den = datum - mesic*100 - rok*10000;
            return new DateTime(rok, mesic, den);
        }

        public static int DateTimeToInt(DateTime dateTime)
        {
            return dateTime.Year*10000 + dateTime.Month*100 + dateTime.Day;
        }
        public static int DateTimeToInt(DateTimeOffset dateTime)
        {
            return dateTime.Year*10000 + dateTime.Month*100 + dateTime.Day;
        }
    }
}
