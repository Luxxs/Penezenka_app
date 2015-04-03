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
    public class RecordsTagsChartMap
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public Color Color { get; set; }
        public double Amount { get; set; }
    }

    public class BalanceDateChartMap
    {
        public DateTime Date { get; set; }
        public double Balance { get; set; }
    }

    public class RecordsViewModel
    {
        public ObservableCollection<Record> Records { get; set; }
        public ObservableCollection<RecordsTagsChartMap> RecordsPerTagChartMap { get; set; }
        public ObservableCollection<BalanceDateChartMap> BalanceInTime { get; set; }
        public double StartBalance { get; set; }
        public double EndBalance { get; set; }
        public double Balance { get; set; }
        public Filter RecordFilter { get; set; }
        public class Filter
        {
            public DateTimeOffset StartDateTime { get; set; }
            public DateTimeOffset EndDateTime { get; set; }
            public bool AllTags { get; set; }
            public List<Tag> Tags { get; set; }
            public bool AllAccounts { get; set; }
            public List<Account> Accounts { get; set; }
            public string GetRecordsWhereClause()
            {
                string whereClause = "WHERE Date>=" + DateTimeToInt(StartDateTime) + " AND Date<=" +
                                     DateTimeToInt(EndDateTime)+" ";
                if (!AllAccounts && Accounts.Count>0)
                {
                    whereClause += "AND Account IN ("+Accounts.First().ID+",";
                    for(int i=1; i<Accounts.Count; i++)
                    {
                        whereClause += "," + Accounts[i].ID;
                    }
                    whereClause += ") ";
                }
                return whereClause;
            }
            public string GetTagsWhereClause()
            {
                if (!AllTags)
                {
                    if (Tags.Count == 0)
                        return "WHERE Tag_ID IS NULL ";
                    string whereClause = "WHERE Tag_ID IN ("+Tags.First().ID+",";
                    for(int i=1; i<Tags.Count; i++)
                    {
                        whereClause += "," + Tags[i].ID;
                    }
                    return whereClause+") ";
                }
                return "";
            }
        }

        private const string recordsSelectSQL = @"SELECT Records.ID, Date, Records.Title, Amount, Records.Notes, Account, Accounts.Title, Accounts.Notes, RecurrenceChain, Type, Value, Disabled, Automatically
                                                        FROM Records
                                                        JOIN Accounts ON Account=Accounts.ID
                                                        JOIN RecurrenceChains ON RecurrenceChain=RecurrenceChains.ID ";

        private string recordsWhereClause = "";
        private const string defaultOrderBy = "ORDER BY Date";

        public void GetFilteredRecords(DateTimeOffset startDate, DateTimeOffset endDate, bool allTags=true, List<Tag> tags=null, bool allAccounts=true, List<Account> accounts=null)
        {
            RecordFilter = new Filter
            {
                StartDateTime = startDate,
                EndDateTime = endDate,
                AllTags = allTags,
                Tags = tags,
                AllAccounts = allAccounts,
                Accounts = accounts
            };
            recordsWhereClause = RecordFilter.GetRecordsWhereClause();
            ISQLiteStatement stmt = DB.Conn.Prepare(recordsSelectSQL + recordsWhereClause);
            getSelectedRecords(stmt);
            stmt = DB.Conn.Prepare(@"SELECT Tags.ID, Tags.Title, Color, sum(Amount)
                                        FROM Records
                                        JOIN RecordsTags ON Records.ID=Record_ID
                                        JOIN Tags ON Tags.ID=Tag_ID " + recordsWhereClause +
                                        ((string.IsNullOrEmpty(recordsWhereClause)) ? "WHERE Amount<0 " : " AND Amount<0 ") +
                                        "GROUP BY Tag_ID " + defaultOrderBy);
            //U PieChart je třeba mapu barev naráz nahradit
            var map = new ObservableCollection<RecordsTagsChartMap>();
            while (stmt.Step() == SQLiteResult.ROW)
            {
                map.Add(new RecordsTagsChartMap{ID=(int)stmt.GetInteger(0), Title=stmt.GetText(1), Color=MyColors.UIntToColor((uint)stmt.GetInteger(2)), Amount=Math.Abs(stmt.GetFloat(3))});
            }
            RecordsPerTagChartMap = map;

            stmt = DB.Conn.Prepare(@"SELECT sum(Amount), Date
                                        FROM Records " + 
                                        recordsWhereClause +
                                        "GROUP BY Date " +
                                        defaultOrderBy);
            ClearBalanceInTime();
            while (stmt.Step() == SQLiteResult.ROW)
            {
                BalanceInTime.Add(new BalanceDateChartMap
                {
                    Balance = stmt.GetFloat(0) + ((BalanceInTime.Count>0) ? BalanceInTime.Last(x=>true).Balance : 0),
                    Date = IntToDateTime((int)stmt.GetInteger(1))
                });
            }
        }

        public void GetRecurrentRecords(bool pending=false)
        {
            var stmt = DB.Conn.Prepare(recordsSelectSQL +
                                       "WHERE RecurrenceChains.ID<>0 AND Disabled<>1 AND Records.ID IN (SELECT max(ID) FROM Records GROUP BY RecurrenceChain)");
            getSelectedRecords(stmt);
            ClearRecords();
            var records = Records;
            foreach (var record in records)
            {
                bool inserted = false;
                DateTimeOffset newRegularDate;
                switch (record.RecurrenceChain.Type)
                {
                    case "W":
                        newRegularDate = record.Date.AddDays(Math.Max(0,6-Misc.DayOfWeekToInt(record.Date.DayOfWeek)));
                        while ((!inserted && pending) || (newRegularDate = newRegularDate.AddDays(1)) <= DateTime.Now)
                        {
                            if ( Misc.DayOfWeekToInt(newRegularDate.DayOfWeek) == record.RecurrenceChain.Value)
                            {
                                record.Date = newRegularDate;
                                Records.Add(record);
                                inserted = true;
                            }
                        }
                        break;
                    case "M":
                        newRegularDate = new DateTime(record.Date.Year, record.Date.Month, (record.RecurrenceChain.Value<29) ? record.RecurrenceChain.Value : 31);
                        while ((!inserted && pending) || (newRegularDate=newRegularDate.AddMonths(1)) <= DateTime.Now)
                        {
                            record.Date = newRegularDate;
                            Records.Add(record);
                            inserted = true;
                        }
                        break;
                    case "Y":
                        int month = record.RecurrenceChain.Value/100;
                        newRegularDate = new DateTime(record.Date.Year, month, month*100 - record.RecurrenceChain.Value);
                        while ((!inserted && pending) || (newRegularDate=newRegularDate.AddYears(1)) <= DateTime.Now)
                        {
                            record.Date = newRegularDate;
                            Records.Add(record);
                            inserted = true;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Get records from <see cref="ISQLiteStatement"/> into <see cref="Records"/> collection
        /// </summary>
        /// <param name="stmt">SELECT statement with optional WHERE clause</param>
        private void getSelectedRecords(ISQLiteStatement stmt)
        {
            ClearRecords();
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

                Records.Add(record);
            }
        }

        private void ClearRecords()
        {
            if(Records == null)
                Records = new ObservableCollection<Record>();
            else
                Records.Clear();
        }

        private void ClearRecordsPerTagChartMap()
        {
            if(RecordsPerTagChartMap == null)
                RecordsPerTagChartMap = new ObservableCollection<RecordsTagsChartMap>();
            else
                RecordsPerTagChartMap.Clear();
        }

        private void ClearBalanceInTime()
        {
            if(BalanceInTime == null)
                BalanceInTime = new ObservableCollection<BalanceDateChartMap>();
            else
                BalanceInTime.Clear();
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
            if (accountIds != null && accountIds.Count > 0)
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
        public static void InsertRecord(int accountId, DateTimeOffset date, string title, double amount, string notes,
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
            stmt.Bind(2, title);
            stmt.Bind(3, amount);
            stmt.Bind(4, notes);
            stmt.Bind(5, accountId);
            stmt.Bind(6, recurrenceChainId);
            stmt.Step();
            stmt.Reset();
            //pozor na transakce
            int recordId = (int)DB.Conn.LastInsertRowId();

            foreach (var tag in tags)
            {
                stmt = DB.Conn.Prepare("INSERT INTO RecordsTags (Record_ID, Tag_ID) VALUES (?,?)");
                stmt.Bind(1,recordId);
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
        //int recordId, int recurrenceChainId
        public void DeleteRecord(Record record)
        {
            var stmt = DB.Conn.Prepare("SELECT count(*) FROM Records WHERE RecurrenceChain=?");
            stmt.Bind(1, record.RecurrenceChain.ID);
            stmt.Step();
            int recordsWithRecurrcenceCount = (int) stmt.GetInteger(0);

            stmt = DB.Conn.Prepare("DELETE FROM RecordsTags WHERE Record_ID=?");
            stmt.Bind(1, record.ID);
            stmt.Step();
            
            stmt = DB.Conn.Prepare("DELETE FROM Records WHERE ID=?");
            stmt.Bind(1, record.ID);
            stmt.Step();

            if (record.RecurrenceChain.ID != 0 && recordsWithRecurrcenceCount == 1)
            {
                stmt = DB.Conn.Prepare("DELETE FROM RecurrenceChains WHERE ID=?");
                stmt.Bind(1, record.RecurrenceChain.ID);
                stmt.Step();
            }
            Records.Remove(record);
        }

        public static void TransferRecord(Record record, int newAccountId)
        {
            UpdateRecord(record.ID, record.Account.ID, record.Date, record.Title, -record.Amount, record.Notes, record.Tags, 0, "", 0);
            InsertRecord(newAccountId, record.Date, record.Title, record.Amount, record.Notes, record.Tags, "", 0, record.RecurrenceChain.ID);
        }

        public void DisableRecurrence(int recurrenceId)
        {
            var stmt = DB.Conn.Prepare("UPDATE RecurrenceChains SET Disabled=1 WHERE ID=?");
            stmt.Bind(1,recurrenceId);
            stmt.Step();
            GetRecurrentRecords(true);
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
