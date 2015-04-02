using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI;
using Penezenka_App.Database;
using Penezenka_App.Model;
using Penezenka_App.OtherClasses;
using SQLitePCL;

namespace Penezenka_App.ViewModel
{
    class RecordsChartMap
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public Color Color { get; set; }
        public double Amount { get; set; }
    }

    class RecordsViewModel
    {
        public ObservableCollection<Record> Records { get; set; }
        public ObservableCollection<RecordsChartMap> RecordsPerTagChartMap { get; set; }

        private readonly string recordsSelectSQL =
            @"SELECT Records.ID, Date, Records.Title, Amount, Records.Notes, Account, Accounts.Title, Accounts.Notes, RecurrenceChain, Type, Value, Disabled, Automatically
                                                        FROM Records
                                                        JOIN Accounts ON Account=Accounts.ID
                                                        JOIN RecurrenceChains ON RecurrenceChain=RecurrenceChains.ID ";
        private string recordsWhereClause = "";

        public void GetMonth(int rok, int mesic)
        {
            recordsWhereClause = "WHERE Date>? AND Date<?";
            ISQLiteStatement stmt = DB.Conn.Prepare(recordsSelectSQL + recordsWhereClause);
            stmt.Bind(1, rok*10000+mesic*100);
            stmt.Bind(2, rok*10000+(mesic+1)*100);
            getSelectedRecords(stmt);

            stmt = DB.Conn.Prepare(@"SELECT Tags.ID, Tags.Title, Color, sum(Amount)
                                        FROM Records
                                        JOIN RecordsTags ON Records.ID=Record_ID
                                        JOIN Tags ON Tags.ID=Tag_ID " + recordsWhereClause +
                                        ((string.IsNullOrEmpty(recordsWhereClause)) ? "WHERE Amount<0" : " AND Amount<0") +
                                        " GROUP BY Tag_ID ");
            stmt.Bind(1, rok*10000+mesic*100);
            stmt.Bind(2, rok*10000+(mesic+1)*100);
            var map = new ObservableCollection<RecordsChartMap>();
            while (stmt.Step() == SQLiteResult.ROW)
            {
                map.Add(new RecordsChartMap{ID=(int)stmt.GetInteger(0), Title=stmt.GetText(1), Color=MyColors.UIntToColor((uint)stmt.GetInteger(2)), Amount=Math.Abs(stmt.GetFloat(3))});
            }
            RecordsPerTagChartMap = map;
        }

        /// <summary>
        /// Get records from <see cref="ISQLiteStatement"/> into <see cref="Records"/> collection
        /// </summary>
        /// <param name="stmt">SELECT statement with optional WHERE clause</param>
        private void getSelectedRecords(ISQLiteStatement stmt)
        {

            ObservableCollection<Record> records = new ObservableCollection<Record>();
            while(stmt.Step() == SQLiteResult.ROW)
            {
                Record record = new Record
                {
                    ID = (int) stmt.GetInteger(0),
                    Date = IntToDateTime((int) stmt.GetInteger(1)),
                    Title = stmt.GetText(2),
                    Amount = stmt.GetFloat(3),
                    Notes = stmt.GetText(4),
                    Account = new Account
                    {
                        ID = (int) stmt.GetInteger(5),
                        Title = stmt.GetText(6),
                        Notes = stmt.GetText(7)
                    },
                    RecurrenceChain = new RecurrenceChain
                    {
                        ID = (int) stmt.GetInteger(8),
                        Type = stmt.GetText(9),
                        Value = (int) stmt.GetInteger(10),
                        Disabled = Convert.ToBoolean(stmt.GetInteger(11))
                    },
                    Automatically = Convert.ToBoolean(stmt.GetInteger(12))
                };

                //Add Tags into record
                ISQLiteStatement tagStmt = DB.Conn.Prepare("SELECT ID, Title, Color, Notes FROM Tags LEFT JOIN RecordsTags ON Tag_ID=ID WHERE Record_ID=?");
                tagStmt.Bind(1,record.ID);
                record.Tags = new List<Tag>();
                while (tagStmt.Step() == SQLiteResult.ROW)
                {
                    record.Tags.Add(new Tag((int)tagStmt.GetInteger(0), tagStmt.GetText(1), (uint)tagStmt.GetInteger(2), tagStmt.GetText(3)));
                }

                records.Add(record);
            }
            Records = records;
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
            List<Tag> tags, string recurrenceType, int recurrenceValue, int recurrenceChainId=0)
        {
            ISQLiteStatement stmt;
            if (recurrenceType != null && recurrenceChainId==0)
            {
                stmt = DB.Conn.Prepare("INSERT INTO RecurrenceChains (Type,Value,Disabled) VALUES (?,?,0)");
                stmt.Bind(1,recurrenceType);
                stmt.Bind(2,recurrenceValue);
                stmt.Step();
                stmt.Reset();
                recurrenceChainId = (int)DB.Conn.LastInsertRowId();
            }

            stmt = DB.Conn.Prepare("INSERT INTO Records (Date,Title,Amount,Notes,Account,RecurrenceChain) VALUES (?,?,?,?,?,?)");
            stmt.Bind(1, DateTimeToInt(date));
            stmt.Bind(2, name);
            stmt.Bind(3, amount);
            stmt.Bind(4, notes);
            stmt.Bind(5, accountId);
            stmt.Bind(6, recurrenceChainId);
            stmt.Step();
            stmt.Reset();
            //pozor na transakce
            int recordID = (int)DB.Conn.LastInsertRowId();

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

        public static void TransferRecord(Record record, int newAccountId)
        {
            UpdateRecord(record.ID, record.Account.ID, record.Date, record.Title, -record.Amount, record.Notes, record.Tags, 0, "", 0);
            InsertRecord(newAccountId, record.Date, record.Title, record.Amount, record.Notes, record.Tags, "", 0, record.RecurrenceChain.ID);
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
