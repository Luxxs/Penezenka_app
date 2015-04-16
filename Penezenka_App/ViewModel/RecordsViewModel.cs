using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Notifications;
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


    public class RecordsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Record> Records { get; set; }
        private ObservableCollection<RecordsTagsChartMap> _expensesPerTagChartMap;
        public ObservableCollection<RecordsTagsChartMap> ExpensesPerTagChartMap
        {
            get { return _expensesPerTagChartMap; }
            set { this.SetProperty(ref this._expensesPerTagChartMap, value); }
        }
        private ObservableCollection<RecordsTagsChartMap> _incomePerTagChartMap;
        public ObservableCollection<RecordsTagsChartMap> IncomePerTagChartMap
        {
            get { return _incomePerTagChartMap; }
            set { this.SetProperty(ref this._incomePerTagChartMap, value); }
        }
        private ObservableCollection<BalanceDateChartMap> _balanceInTime;
        public ObservableCollection<BalanceDateChartMap> BalanceInTime
        {
            get { return _balanceInTime; }
            set { this.SetProperty(ref this._balanceInTime, value); }
        }
        private double _selectedExpenses;
        public double SelectedExpenses
        {
            get { return _selectedExpenses; }
            set { this.SetProperty(ref this._selectedExpenses, value); }
        }
        private double _selectedIncome;
        public double SelectedIncome
        {
            get { return _selectedIncome; }
            set { this.SetProperty(ref this._selectedIncome, value); }
        }
        private double _balance;
        public double Balance
        {
            get { return _balance; }
            set { this.SetProperty(ref this._balance, value); }
        }
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
                string whereClause = " WHERE Date>=" + DateTimeToInt(StartDateTime) + " AND Date<=" +
                                     DateTimeToInt(EndDateTime)+" ";
                if (!AllAccounts && Accounts!=null && Accounts.Count>0)
                {
                    whereClause += " AND Account IN ("+Accounts.First().ID;
                    for(int i=1; i<Accounts.Count; i++)
                    {
                        whereClause += "," + Accounts[i].ID;
                    }
                    whereClause += ")";
                }
                return whereClause;
            }
            public string GetTagsWhereClause()
            {
                /*if (Tags==null || Tags.Count == 0)
                    return " AND Tag_ID IS NULL";*/
                if (!AllTags && Tags!=null && Tags.Count>0)
                {
                    string whereClause = " AND Tag_ID IN ("+Tags.First().ID;
                    for(int i=1; i<Tags.Count; i++)
                    {
                        whereClause += "," + Tags[i].ID;
                    }
                    return whereClause+")";
                }
                if (!AllTags && Tags != null && Tags.Count == 0)
                {
                    return " AND Tag_ID IS NULL";
                }
                return "";
            }
        }

        private const string recordsSelectSQL = @"SELECT Records.ID, Date, Records.Title, Amount, Records.Notes, Account, Accounts.Title, Accounts.Notes, RecurrenceChain, Type, Value, Disabled, Automatically
                                                        FROM Records
                                                        JOIN Accounts ON Account=Accounts.ID
                                                        JOIN RecurrenceChains ON RecurrenceChain=RecurrenceChains.ID";

        //private string recordsWhereClause = "";
        private const string defaultOrderBy = " ORDER BY Date";

        public void GetFilteredRecords(Filter filter)
        {
            RecordFilter = filter;
            string recordsWhereClause = RecordFilter.GetRecordsWhereClause();
            string tagsWhereClause = RecordFilter.GetTagsWhereClause();
            ISQLiteStatement stmt = DB.Conn.Prepare(recordsSelectSQL + recordsWhereClause);
            getSelectedRecords(stmt);

            GetGroupedRecordsPerTag();
            GetGroupedRecordsPerTag(true);

            stmt = DB.Conn.Prepare("SELECT sum(Amount) FROM Records");
            stmt.Step();
            try
            {
                Balance = stmt.GetFloat(0);
            }
            catch (SQLiteException ex)
            {
                Balance = 0;
            }
            SelectedExpenses = Records.Sum(rec => (rec.Amount < 0) ? rec.Amount : 0);
            SelectedIncome = Records.Sum(rec => (rec.Amount > 0) ? rec.Amount : 0);

            double preBalance = 0;
            stmt = DB.Conn.Prepare("SELECT sum(Amount) FROM Records WHERE Date < ?");
            stmt.Bind(1, DateTimeToInt(filter.StartDateTime));
            stmt.Step();
            try
            {
                preBalance = stmt.GetFloat(0);
            }
            catch (SQLiteException ex) { }
            stmt = DB.Conn.Prepare(@"SELECT sum(Amount), Date
                                          FROM Records" + 
                                        //" WHERE ID IN (" + recordIds + ")" +
                                        //recordsWhereClause +
                                        " WHERE Date>=? AND Date<=?" +
                                        " GROUP BY Date " +
                                        defaultOrderBy);
            stmt.Bind(1, DateTimeToInt(filter.StartDateTime));
            stmt.Bind(2, DateTimeToInt(filter.EndDateTime));
            ClearBalanceInTime();
            var balance = new ObservableCollection<BalanceDateChartMap>();
            while (stmt.Step() == SQLiteResult.ROW)
            {
                balance.Add(new BalanceDateChartMap
                {
                    Balance = stmt.GetFloat(0) + ((BalanceInTime.Count==0) ? preBalance : BalanceInTime.Last(x=>true).Balance),
                    Date = IntToDateTime((int)stmt.GetInteger(1))
                });
            }
            BalanceInTime = balance;
        }

        public void GetRecurrentRecords(bool pending=false)
        {
            var stmt = DB.Conn.Prepare(recordsSelectSQL +
                                       " WHERE RecurrenceChains.ID<>0 AND Disabled<>1 AND Records.ID IN (SELECT max(ID) FROM Records GROUP BY RecurrenceChain)");
            getSelectedRecords(stmt);
            var records = new ObservableCollection<Record>(Records);
            Records.Clear();
            foreach (var record in records)
            {
                record.Automatically = true;
                DateTimeOffset newRegularDate;
                switch (record.RecurrenceChain.Type)
                {
                    case "W":
                        int daysTo = record.RecurrenceChain.Value - Misc.DayOfWeekToInt(record.Date.DayOfWeek);
                        daysTo = (daysTo < 0) ? 7 + daysTo : daysTo;
                        newRegularDate = record.Date.AddDays(((daysTo==0) ? 7 : daysTo));
                        while (newRegularDate <= DateTime.Now)
                        {
                            if (!pending /*&& Misc.DayOfWeekToInt(newRegularDate.DayOfWeek) == record.RecurrenceChain.Value*/)
                            {
                                record.Date = newRegularDate;
                                Records.Add(record);
                            }
                            newRegularDate = newRegularDate.AddDays(7);
                        }
                        if (pending)
                        {
                            if (Misc.DayOfWeekToInt(newRegularDate.DayOfWeek) > Misc.DayOfWeekToInt(DateTime.Now.DayOfWeek))
                                record.Date = newRegularDate.AddDays(7);
                            else
                                record.Date = newRegularDate;
                            Records.Add(record);
                        }
                        break;
                    case "M":
                        newRegularDate = new DateTime(record.Date.Year, record.Date.Month, (record.RecurrenceChain.Value<29) ? record.RecurrenceChain.Value : 31);
                        while ((newRegularDate=newRegularDate.AddMonths(1)) <= DateTime.Now)
                        {
                            if (!pending)
                            {
                                record.Date = newRegularDate;
                                Records.Add(record);
                            }
                        }
                        if (pending)
                        {
                            record.Date = newRegularDate;
                            Records.Add(record);
                        }
                        break;
                    case "Y":
                        int month = record.RecurrenceChain.Value/100;
                        newRegularDate = new DateTime(record.Date.Year, month, record.RecurrenceChain.Value - month*100);
                        while ((newRegularDate=newRegularDate.AddYears(1)) <= DateTime.Now)
                        {
                            if (!pending)
                            {
                                record.Date = newRegularDate;
                                Records.Add(record);
                            }
                        }
                        if (pending)
                        {
                            record.Date = newRegularDate;
                            Records.Add(record);
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
                bool accountCorrect = false;
                bool tagsCorrect = false;
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
                if ((RecordFilter == null || RecordFilter.AllAccounts) ||
                    RecordFilter != null && !RecordFilter.AllAccounts && RecordFilter.Accounts != null &&
                    RecordFilter.Accounts.Contains(record.Account))
                    accountCorrect = true;
                try
                {
                    if (RecordFilter!=null && !RecordFilter.AllTags)
                    {
                        var hasTagStmt =
                            DB.Conn.Prepare("SELECT ID FROM Records LEFT JOIN RecordsTags ON Record_ID=ID WHERE ID=?" +
                                            RecordFilter.GetTagsWhereClause());
                        hasTagStmt.Bind(1, record.ID);
                        hasTagStmt.Step();
                        int isResultNull = (int) hasTagStmt.GetInteger(0);
                        //if (RecordFilter.Tags != null && RecordFilter.Tags.Count > 0)
                            tagsCorrect = true;
                    }
                    else
                    {
                        tagsCorrect = true;
                    }
                }
                catch (SQLiteException)
                {
                    //if (RecordFilter.Tags == null || RecordFilter.Tags.Count == 0)
                    //    tagsCorrect = true;
                }
                if (tagsCorrect && accountCorrect)
                {
                    //Add Tags into record
                    ISQLiteStatement tagStmt =
                        DB.Conn.Prepare(
                            "SELECT ID, Title, Color, Notes FROM Tags LEFT JOIN RecordsTags ON Tag_ID=ID WHERE Record_ID=?");
                    tagStmt.Bind(1, record.ID);
                    record.Tags = new List<Tag>();
                    while (tagStmt.Step() == SQLiteResult.ROW)
                    {
                        record.Tags.Add(new Tag((int) tagStmt.GetInteger(0), tagStmt.GetText(1),
                            (uint) tagStmt.GetInteger(2), tagStmt.GetText(3)));
                    }

                    Records.Add(record);
                }
            }
        }
        private void GetGroupedRecordsPerTag(bool income = false)
        {
            var stmt = DB.Conn.Prepare(@"SELECT Tags.ID, Tags.Title, Color, sum(Amount)
                                        FROM Records
                                        LEFT JOIN (SELECT * FROM RecordsTags
                                        JOIN Tags ON ID=Tag_ID) Tags ON Records.ID=Record_ID " +
                                        RecordFilter.GetRecordsWhereClause() + RecordFilter.GetTagsWhereClause() + ((income) ? " AND Amount>0" : " AND Amount<0") +
                                        " GROUP BY Tag_ID " + defaultOrderBy);
            //U PieChart je třeba mapu barev naráz nahradit
            var map = new ObservableCollection<RecordsTagsChartMap>();
            while (stmt.Step() == SQLiteResult.ROW)
            {
                try
                {
                    map.Add(new RecordsTagsChartMap
                    {
                        ID = (int) stmt.GetInteger(0),
                        Title = stmt.GetText(1),
                        Color = MyColors.UIntToColor((uint) stmt.GetInteger(2)),
                        Amount = Math.Abs(stmt.GetFloat(3))
                    });
                }
                catch (SQLiteException)
                {
                    map.Add(new RecordsTagsChartMap
                    {
                        ID = 0,
                        Title = "Bez štítků",
                        Color = Colors.Gray,
                        Amount = Math.Abs(stmt.GetFloat(3))
                    });
                }
            }
            if (income)
                IncomePerTagChartMap = map;
            else
                ExpensesPerTagChartMap = map;
        }

        private void ClearRecords()
        {
            if(Records == null)
                Records = new ObservableCollection<Record>();
            else
                Records.Clear();
        }
        private void ClearBalanceInTime()
        {
            if(BalanceInTime == null)
                BalanceInTime = new ObservableCollection<BalanceDateChartMap>();
            else
                BalanceInTime.Clear();
        }

        public static DateTimeOffset GetMinDate()
        {
            try
            {
                ISQLiteStatement stmt = DB.Conn.Prepare("SELECT min(Date) FROM Records");
                stmt.Step();
                return IntToDateTime((int)stmt.GetInteger(0));
            }
            catch (SQLiteException)
            {
                return DateTimeOffset.MinValue;
            }
        }
        public static DateTimeOffset GetMaxDate()
        {
            try {
            ISQLiteStatement stmt = DB.Conn.Prepare("SELECT min(Date) FROM Records");
            stmt.Step();
            return IntToDateTime((int)stmt.GetInteger(0));
            }
            catch (SQLiteException)
            {
                return DateTimeOffset.MaxValue;
            }
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
            //úprava Balance (MVVM) zde zatím není potřeba
        }
        public static void UpdateRecord(int recordId, int accountId, DateTimeOffset date, string name, double amount, string notes,
            List<Tag> tags, int recurrenceChainId, string recurrenceType, int recurrenceValue, int automatically=0)
        {
            ISQLiteStatement stmt;

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
            stmt = DB.Conn.Prepare("UPDATE Records SET Date=?, Title=?, Amount=?, Notes=?, Account=?, RecurrenceChain=?, Automatically=0 WHERE ID=?");
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
            //úprava Balance (MVVM) zde zatím není potřeba (probíhá z jiné stránky)
        }
        //int recordId, int recurrenceChainId
        public bool DeleteRecord(Record record)
        {
            bool disabledRecurrence = false;
            var stmt = DB.Conn.Prepare("SELECT count(*) FROM Records WHERE RecurrenceChain=?");
            stmt.Bind(1, record.RecurrenceChain.ID);
            stmt.Step();
            int recordsWithRecurrenceCount = (int) stmt.GetInteger(0);

            stmt = DB.Conn.Prepare("SELECT max(ID) FROM Records WHERE RecurrenceChain=?");
            stmt.Bind(1, record.RecurrenceChain.ID);
            stmt.Step();
            int recurrenceMaxRecordId = (int) stmt.GetInteger(0);

            stmt = DB.Conn.Prepare("DELETE FROM RecordsTags WHERE Record_ID=?");
            stmt.Bind(1, record.ID);
            stmt.Step();
            
            stmt = DB.Conn.Prepare("DELETE FROM Records WHERE ID=?");
            stmt.Bind(1, record.ID);
            stmt.Step();

            if (record.RecurrenceChain.ID != 0 && recordsWithRecurrenceCount == 1)
            {
                stmt = DB.Conn.Prepare("DELETE FROM RecurrenceChains WHERE ID=?");
                stmt.Bind(1, record.RecurrenceChain.ID);
                stmt.Step();
                disabledRecurrence = true;
            }
            else if (recurrenceMaxRecordId == record.ID)
            {
                DisableRecurrence(record.RecurrenceChain.ID, true);
                disabledRecurrence = true;
            }
            var tagIds = new List<int>(record.Tags.Select(tag => tag.ID));
            if(tagIds.Count==0)
                tagIds.Add(0);
            if (record.Amount < 0)
            {
                var newTagMap = new ObservableCollection<RecordsTagsChartMap>(ExpensesPerTagChartMap);
                foreach (var tagMap in ExpensesPerTagChartMap)
                {
                    if (tagIds.Contains(tagMap.ID))
                    {
                        double absAmount = Math.Abs(record.Amount);
                        if (tagMap.Amount - absAmount > 0)
                        {
                            tagMap.Amount -= absAmount;
                        }
                        else
                        {
                            newTagMap.Remove(tagMap);
                        }
                    }
                }
                //Pozor, nahrazením instance přestane fungovat DataBinding! - kvůli PieChart nelze přímo volat Remove → hlásí to nějaké chyby
                ExpensesPerTagChartMap = newTagMap;
            }
            else
            {
                var newTagMap = new ObservableCollection<RecordsTagsChartMap>(IncomePerTagChartMap);
                foreach (var tagMap in IncomePerTagChartMap)
                {
                    if (tagIds.Contains(tagMap.ID))
                    {
                        double absAmount = Math.Abs(record.Amount);
                        if (tagMap.Amount - absAmount > 0)
                        {
                            tagMap.Amount -= absAmount;
                        }
                        else
                        {
                            newTagMap.Remove(tagMap);
                        }
                    }
                }
                IncomePerTagChartMap = newTagMap;
            }
            Records.Remove(record);
            var balance = new ObservableCollection<BalanceDateChartMap>(BalanceInTime);
            foreach (var balanceItem in BalanceInTime)
            {
                if (Records.FirstOrDefault(x => x.Date == balanceItem.Date) == null)
                {
                    balance.Remove(balanceItem);
                }
                else
                {
                    balanceItem.Balance -= record.Amount;
                }
            }
            BalanceInTime = balance;
            if (record.Amount < 0)
                SelectedExpenses -= record.Amount;
            else
                SelectedIncome -= record.Amount;
            Balance -= record.Amount;
            return disabledRecurrence;
        }

        public void DisableRecurrence(int recurrenceId, bool fromDeleteRecord=false)
        {
            var stmt = DB.Conn.Prepare("UPDATE RecurrenceChains SET Disabled=1 WHERE ID=?");
            stmt.Bind(1,recurrenceId);
            stmt.Step();
            if(!fromDeleteRecord)
                Records.Remove(Records.First(x => x.RecurrenceChain.ID == recurrenceId));
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


        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value))
                return false;
            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
