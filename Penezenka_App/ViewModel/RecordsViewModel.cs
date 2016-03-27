using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Windows.UI;
using Penezenka_App.Database;
using Penezenka_App.Model;
using Penezenka_App.OtherClasses;
using SQLitePCL;

namespace Penezenka_App.ViewModel
{
    public enum RecordSearchArea
    {
        All, Filter, Displayed
    }
    public class RecordsViewModel : INotifyPropertyChanged
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
        [DataContract]
        public class Filter
        {
            [DataMember]
            public DateTimeOffset StartDateTime { get; set; }
            [DataMember]
            public DateTimeOffset EndDateTime { get; set; }
            [DataMember]
            public bool AllTags { get; set; }
            [DataMember]
            public List<Tag> Tags { get; set; }
            [DataMember]
            public bool AllAccounts { get; set; }
            [DataMember]
            public List<Account> Accounts { get; set; }
            public Filter()
            {
                StartDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                EndDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1);
                AllTags = true;
                AllAccounts = true;
            }
            public string GetRecordsWhereClause()
            {
                string whereClause = " WHERE Date>=" + Misc.DateTimeToInt(StartDateTime) + " AND Date<=" + Misc.DateTimeToInt(EndDateTime) + " ";
                if (!AllAccounts && Accounts != null && Accounts.Count > 0)
                {
                    whereClause += " AND Account IN (" + Accounts.First().ID;
                    for (int i = 1; i < Accounts.Count; i++)
                    {
                        whereClause += "," + Accounts[i].ID;
                    }
                    whereClause += ")";
                }
                return whereClause;
            }
            public string GetTagsWhereClause()
            {
                if (!AllTags && Tags != null)
                {
                    if (Tags.Count > 0)
                    {
                        string join = string.Join(",", Tags.Select(x => x.ID));
                        return " AND Tag_ID IN (" + join + ")";
                    }
                    if (Tags.Count == 0)
                    {
                        return " AND Tag_ID IS NULL";
                    }
                }
                return "";
            }
        }
        private class RecordsEnumerator
        {
            private ISQLiteStatement stmt;
            public RecordsEnumerator(ISQLiteStatement stmt)
            {
                this.stmt = stmt;
            }
            public IEnumerator<Record> GetEnumerator()
            {
                while (stmt.Step() == SQLiteResult.ROW)
                {
                    Record record = new Record
                    {
                        ID = (int)stmt.GetInteger(0),
                        Date = Misc.IntToDateTime((int)stmt.GetInteger(1)),
                        Title = stmt.GetText(2),
                        Amount = stmt.GetFloat(3),
                        Notes = stmt.GetText(4),
                        Account = new Account
                        {
                            ID = (int)stmt.GetInteger(5),
                            Title = stmt.GetText(6),
                            Notes = stmt.GetText(7)
                        },
                        RecurrenceChain = new RecurrenceChain
                        {
                            ID = (int)stmt.GetInteger(8),
                            Type = stmt.GetText(9),
                            Value = (int)stmt.GetInteger(10),
                            Disabled = Convert.ToBoolean(stmt.GetInteger(11))
                        },
                        Automatically = Convert.ToBoolean(stmt.GetInteger(12))
                    };
                    using (ISQLiteStatement tagStmt =
                        DB.Query(
                            "SELECT ID, Title, Color, Notes FROM Tags LEFT JOIN RecordsTags ON Tag_ID=ID WHERE Record_ID=?",
                            record.ID))
                    {
                        record.Tags = new List<Tag>();
                        while (tagStmt.Step() == SQLiteResult.ROW)
                        {
                            record.Tags.Add(new Tag((int)tagStmt.GetInteger(0), tagStmt.GetText(1),
                                (uint)tagStmt.GetInteger(2), tagStmt.GetText(3)));
                        }
                    }
                    yield return record;
                }
            }
        }


        public ObservableCollection<Record> Records { get; set; }
        private int _recordsSorting = 0;
        public int RecordsSorting
        {
            get { return _recordsSorting; }
            set
            {
                SortRecords(value);
                _recordsSorting = value;
            }
        }

        private ObservableCollection<RecordsTagsChartMap> _expensesPerTagChartMap;
        public ObservableCollection<RecordsTagsChartMap> ExpensesPerTagChartMap
        {
            get { return _expensesPerTagChartMap; }
            set { SetProperty(ref _expensesPerTagChartMap, value); }
        }
        private ObservableCollection<RecordsTagsChartMap> _incomePerTagChartMap;
        public ObservableCollection<RecordsTagsChartMap> IncomePerTagChartMap
        {
            get { return _incomePerTagChartMap; }
            set { SetProperty(ref _incomePerTagChartMap, value); }
        }
        private ObservableCollection<BalanceDateChartMap> _balanceInTime;
        public ObservableCollection<BalanceDateChartMap> BalanceInTime
        {
            get { return _balanceInTime; }
            set { SetProperty(ref _balanceInTime, value); }
        }
        private double _selectedExpenses;
        public double SelectedExpenses
        {
            get { return _selectedExpenses; }
            set { SetProperty(ref _selectedExpenses, value); }
        }
        private double _selectedIncome;
        public double SelectedIncome
        {
            get { return _selectedIncome; }
            set { SetProperty(ref _selectedIncome, value); }
        }
        private double _balance;
        public double Balance
        {
            get { return _balance; }
            set { SetProperty(ref _balance, value); }
        }
        private int _foundCount = 0;
        public int FoundCount
        {
            get { return _foundCount; }
            set { SetProperty(ref _foundCount, value); }
        }
        public Filter RecordFilter { get; set; }
        private const string recordsSelectSQL = @"SELECT Records.ID, Date, Records.Title, Amount, Records.Notes, Account, Accounts.Title, Accounts.Notes, RecurrenceChain, Type, Value, Disabled, Automatically
                                                        FROM Records
                                                        JOIN Accounts ON Account=Accounts.ID
                                                        JOIN RecurrenceChains ON RecurrenceChain=RecurrenceChains.ID";
        private const string defaultOrderBy = " ORDER BY Date DESC";


        /* ==============================
                PUBLIC METHODS
        ============================== */
        public void GetFilteredRecords(Filter filter)
        {
            ClearRecords();
            RecordFilter = filter;
            using (ISQLiteStatement stmt = DB.Query(recordsSelectSQL + RecordFilter.GetRecordsWhereClause() + ((RecordsSorting == 0) ? defaultOrderBy : "")))
            {
                foreach (var record in new RecordsEnumerator(stmt))
                {
                    if (RecordFilter == null || RecordFilter.AllTags || (record.Tags.Count == 0 && RecordFilter.Tags.Count == 0) || (RecordFilter.Tags.Count > 0 && !RecordFilter.Tags.Any(filterTag => !record.Tags.Contains(filterTag))))
                    {
                        Records.Add(record);
                    }
                }
            }

            GetGroupedRecordsPerTag();
            GetGroupedRecordsPerTag(true);

            GetBalances();

            if (RecordsSorting > 0)
                SortRecords(RecordsSorting);
        }

        public void GetRecurrentRecords(bool pending = false)
        {
            using (var stmt = DB.Query(recordsSelectSQL +
                                       @" WHERE RecurrenceChains.ID<>0 AND Disabled<>1 AND
                                            Records.ID IN (SELECT max(ID) FROM Records GROUP BY RecurrenceChain)"))
            {

                ClearRecords();
                foreach (var record in new RecordsEnumerator(stmt))
                {
                    record.Automatically = true;
                    DateTimeOffset newRegularDate;
                    switch (record.RecurrenceChain.Type)
                    {
                        case "W":
                            int daysTo = record.RecurrenceChain.Value - Misc.DayOfWeekToInt(record.Date.DayOfWeek) + 7;
                            newRegularDate = record.Date.AddDays(daysTo);
                            while (newRegularDate <= DateTime.Now)
                            {
                                if (!pending)
                                {
                                    var novy = new Record(record) { Date = newRegularDate };
                                    Records.Add(novy);
                                }
                                newRegularDate = newRegularDate.AddDays(7);
                            }
                            if (pending)
                            {
                                var novy = new Record(record);
                                if (Misc.DayOfWeekToInt(newRegularDate.DayOfWeek) > Misc.DayOfWeekToInt(DateTime.Now.DayOfWeek))
                                    novy.Date = newRegularDate.AddDays(7);
                                else
                                    novy.Date = newRegularDate;
                                Records.Add(novy);
                            }
                            break;
                        case "M":
                            if (record.RecurrenceChain.Value == 29) //29 means last day in month
                            {
                                newRegularDate =
                                    new DateTime(record.Date.Year, record.Date.Month, 1).AddMonths(2).AddDays(-1);
                            }
                            else
                            {
                                newRegularDate = new DateTime(record.Date.Year, record.Date.Month, record.RecurrenceChain.Value).AddMonths(1);
                            }
                            while (newRegularDate <= DateTime.Now)
                            {
                                if (!pending)
                                {
                                    var novy = new Record(record) { Date = newRegularDate };
                                    Records.Add(novy);
                                }
                                if (record.RecurrenceChain.Value == 29)
                                {
                                    newRegularDate = new DateTime(newRegularDate.Year, newRegularDate.Month, 1).AddMonths(2).AddDays(-1);
                                }
                                else
                                {
                                    newRegularDate = newRegularDate.AddMonths(1);
                                }
                            }
                            if (pending)
                            {
                                var novy = new Record(record) { Date = newRegularDate };
                                Records.Add(novy);
                            }
                            break;
                        case "Y":
                            int month = record.RecurrenceChain.Value / 100;
                            int day = record.RecurrenceChain.Value - month * 100;
                            if (day == 29)
                            {
                                newRegularDate = new DateTime(record.Date.Year + 1, month, 1).AddMonths(1).AddDays(-1);
                            }
                            else
                            {
                                newRegularDate = new DateTime(record.Date.Year + 1, month, day);
                            }
                            while (newRegularDate <= DateTime.Now)
                            {
                                if (!pending)
                                {
                                    var novy = new Record(record) { Date = newRegularDate };
                                    Records.Add(novy);
                                }
                                if (day == 29)
                                {
                                    newRegularDate = new DateTime(newRegularDate.Year + 1, month, 1).AddMonths(1).AddDays(-1);
                                }
                                else
                                {
                                    newRegularDate = newRegularDate.AddYears(1);
                                }
                            }
                            if (pending)
                            {
                                var novy = new Record(record) { Date = newRegularDate };
                                Records.Add(novy);
                            }
                            break;
                    }
                }
            }
            if (pending)
            {
                RecordsSorting = 1;
            }
        }

        public void GetSearchedRecords(string phrase, bool inTitles, bool inNotes, RecordSearchArea area, bool onlyCount)
        {
            if ((inTitles || inNotes) && !string.IsNullOrEmpty(phrase))
            {
                switch (area)
                {
                    case RecordSearchArea.Filter:
                        if (!onlyCount)
                            ClearRecords();

                        FoundCount = 0;
                        using (ISQLiteStatement stmt1 = DB.Query(recordsSelectSQL + RecordFilter.GetRecordsWhereClause()))
                        {
                            foreach (var record in new RecordsEnumerator(stmt1))
                            {
                                if ((RecordFilter == null || RecordFilter.AllTags || (record.Tags.Count == 0 && RecordFilter.Tags.Count == 0) || RecordFilter.Tags.Any(filterTag => !record.Tags.Contains(filterTag)))
                                    && inTitles && record.Title.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                            inNotes && record.Notes.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    if (!onlyCount)
                                        Records.Add(record);
                                    FoundCount++;
                                }
                            }
                        }
                        break;
                    case RecordSearchArea.All:
                        string where = " WHERE ";
                        if (inTitles)
                        {
                            where += "Records.Title LIKE '%' || ? || '%'";
                        }
                        if (inTitles && inNotes)
                        {
                            where += " OR ";
                        }
                        if (inNotes)
                        {
                            where += "Records.Notes LIKE '%' || ? || '%'";
                        }
                        ISQLiteStatement stmt;
                        if (onlyCount)
                            stmt = (inTitles && inNotes) ? DB.Query("SELECT count(*) FROM Records" + where, phrase, phrase) : DB.Query("SELECT count(*) FROM Records" + where, phrase);
                        else
                            stmt = (inTitles && inNotes) ? DB.Query(recordsSelectSQL + where, phrase, phrase) : DB.Query(recordsSelectSQL + where, phrase);

                        if (onlyCount)
                        {
                            using (stmt)
                            {
                                try
                                {
                                    stmt.Step();
                                    FoundCount = (int)stmt.GetInteger(0);
                                }
                                catch (SQLiteException)
                                {
                                    FoundCount = 0;
                                }
                            }
                        }
                        else
                        {
                            using (stmt)
                            {
                                ClearRecords();
                                foreach (var record in new RecordsEnumerator(stmt))
                                {
                                    Records.Add(record);
                                }
                            }
                            FoundCount = Records.Count();
                        }
                        break;
                    case RecordSearchArea.Displayed:
                        var foundRecords = Records.Where(x => inTitles && x.Title.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                      inNotes && x.Notes.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                        if (!onlyCount)
                        {
                            ClearRecords();
                            foreach (var found in foundRecords)
                            {
                                Records.Add(found);
                            }
                            FoundCount = Records.Count();
                        }
                        else
                        {
                            FoundCount = foundRecords.Count();
                        }
                        break;
                }
                if (!onlyCount)
                    SortRecords(RecordsSorting);
            }
            else
            {
                FoundCount = 0;
                if (!onlyCount)
                    ClearRecords();
            }
            if (!onlyCount)
            {
                GetGroupedRecordsPerTag();
                GetGroupedRecordsPerTag(true);
                ClearBalanceInTime();
                GetBalances();
            }
        }
        public void GetAllRecords(string orderBy = "")
        {
            ClearRecords();
            using (ISQLiteStatement stmt = DB.Query(recordsSelectSQL + " " + orderBy))
            {
                foreach (var record in new RecordsEnumerator(stmt))
                {
                    Records.Add(record);
                }
            }
        }

        public static Record GetRecordByID(int id)
        {
            using (var stmt = DB.Query(recordsSelectSQL + " WHERE Records.ID=?", id))
            {
                foreach (var record in new RecordsEnumerator(stmt))
                {
                    return record;
                }
            }
            return null;
        }

        public static DateTimeOffset GetMinDate()
        {
            try
            {
                using (ISQLiteStatement stmt = DB.Query("SELECT min(Date) FROM Records"))
                {
                    stmt.Step();
                    return Misc.IntToDateTime((int)stmt.GetInteger(0));
                }
            }
            catch (SQLiteException)
            {
                return DateTimeOffset.MinValue;
            }
        }
        public static DateTimeOffset GetMaxDate()
        {
            try
            {
                using (ISQLiteStatement stmt = DB.Query("SELECT max(Date) FROM Records"))
                {
                    stmt.Step();
                    return Misc.IntToDateTime((int)stmt.GetInteger(0));
                }
            }
            catch (SQLiteException)
            {
                return DateTimeOffset.MaxValue;
            }
        }

        /* PRIVATE METHODS */
        private void GetGroupedRecordsPerTag(bool income = false)
        {
            var expandedRecords = new ObservableCollection<RecordsTagsChartMap>();
            foreach (var record in Records.Where(x => income && x.Amount > 0 || !income && x.Amount < 0))
            {
                if (record.Tags.Count == 0)
                {
                    expandedRecords.Add(new RecordsTagsChartMap() { ID = 0, Color = Colors.Gray, Title = "Bez štítků", Amount = record.Amount });
                }
                else
                {
                    foreach (var tag in record.Tags)
                    {
                        expandedRecords.Add(new RecordsTagsChartMap() { ID = tag.ID, Color = tag.Color.Color, Title = tag.Title, Amount = record.Amount });
                    }
                }
            }
            var map = (from record in expandedRecords
                       group record by record.ID into recordGroup
                       orderby recordGroup.Sum(x => Math.Abs(x.Amount)) descending
                       select new RecordsTagsChartMap()
                       {
                           ID = recordGroup.First().ID,
                           Color = recordGroup.First().Color,
                           Title = recordGroup.First().Title,
                           Amount = recordGroup.Sum(x => x.Amount)
                       }).ToArray();
            double sum = map.Sum(x => x.Amount);
            foreach (var item in map)
            {
                item.Title += string.Format(" ({0:0.0 %})", item.Amount / sum);
            }
            if (income)
                IncomePerTagChartMap = new ObservableCollection<RecordsTagsChartMap>(map);
            else
                ExpensesPerTagChartMap = new ObservableCollection<RecordsTagsChartMap>(map);
        }
        private void GetBalances()
        {
            using (var stmt = DB.Query("SELECT sum(Amount) FROM Records"))
            {
                stmt.Step();
                try
                {
                    Balance = stmt.GetFloat(0);
                }
                catch (SQLiteException)
                {
                    Balance = 0;
                }
                SelectedExpenses = Records.Sum(rec => (rec.Amount < 0) ? rec.Amount : 0);
                SelectedIncome = Records.Sum(rec => (rec.Amount > 0) ? rec.Amount : 0);
            }

            double preBalance = 0;
            using (var stmt = DB.Query("SELECT sum(Amount) FROM Records WHERE Date < ?", Misc.DateTimeToInt(RecordFilter.StartDateTime)))
            {
                stmt.Step();
                try
                {
                    preBalance = stmt.GetFloat(0);
                }
                catch (SQLiteException) { }
            }

            using (var stmt = DB.Query(@"SELECT sum(Amount), Date
                                FROM Records
                                WHERE Date>=? AND Date<=?
                                GROUP BY Date ORDER BY Date", Misc.DateTimeToInt(RecordFilter.StartDateTime), Misc.DateTimeToInt(RecordFilter.EndDateTime)))
            {
                ClearBalanceInTime();
                while (stmt.Step() == SQLiteResult.ROW)
                {
                    BalanceInTime.Add(new BalanceDateChartMap
                    {
                        Balance = stmt.GetFloat(0) + ((BalanceInTime.Count == 0) ? preBalance : BalanceInTime.Last(x => true).Balance),
                        Date = Misc.IntToDateTime((int)stmt.GetInteger(1))
                    });
                }
            }
        }

        private void SortRecords(int sortBy)
        {
            Record[] sortedRecords = new Record[Records.Count];
            switch (sortBy)
            {
                case 0:
                    sortedRecords = Records.OrderByDescending(x => x.Date).ToArray();
                    break;
                case 1:
                    sortedRecords = Records.OrderBy(x => x.Date).ToArray();
                    break;
                case 2:
                    sortedRecords = Records.OrderByDescending(x => Math.Abs(x.Amount)).ToArray();
                    break;
                case 3:
                    sortedRecords = Records.OrderBy(x => Math.Abs(x.Amount)).ToArray();
                    break;
                case 4:
                    sortedRecords = Records.OrderBy(x => x.Title).ToArray();
                    break;
                case 5:
                    sortedRecords = Records.OrderByDescending(x => x.Title).ToArray();
                    break;
            }
            ClearRecords();
            foreach (var rec in sortedRecords)
            {
                Records.Add(rec);
            }
        }


        /* INSERT, UPDATE, DELETE */
        public static void InsertRecord(int accountId, DateTimeOffset date, string title, double amount, string notes,
            List<Tag> tags, string recurrenceType, int recurrenceValue, int recurrenceChainId = 0)
        {
            if (recurrenceType != null && recurrenceChainId == 0)
            {
                DB.QueryAndStep("INSERT INTO RecurrenceChains (Type,Value,Disabled) VALUES (?,?,0)", recurrenceType,
                    recurrenceValue);
                recurrenceChainId = (int)DB.Conn.LastInsertRowId();
            }

            DB.QueryAndStep(
                "INSERT INTO Records (Date,Title,Amount,Notes,Account,RecurrenceChain) VALUES (?,?,?,?,?,?)",
                Misc.DateTimeToInt(date), title, amount, notes, accountId, recurrenceChainId);
            int recordId = (int)DB.Conn.LastInsertRowId();

            foreach (var tag in tags)
            {
                DB.QueryAndStep("INSERT INTO RecordsTags (Record_ID, Tag_ID) VALUES (?,?)", recordId, tag.ID);
            }
        }
        public static void UpdateRecord(int recordId, int accountId, DateTimeOffset date, string name, double amount, string notes,
            List<Tag> tags, int recurrenceChainId, string recurrenceType, int recurrenceValue)
        {
            if (recurrenceChainId != 0)
            {
                if (recurrenceType == null)
                {
                    DB.QueryAndStep("UPDATE RecurrenceChains SET Disabled=1 WHERE ID=?", recurrenceChainId);
                }
                else
                {
                    DB.QueryAndStep("UPDATE RecurrenceChains SET Type=?, Value=?, Disabled=0 WHERE ID=?",
                        recurrenceType, recurrenceValue, recurrenceChainId);
                }
            }
            else if (recurrenceType != null)
            {
                DB.QueryAndStep("INSERT INTO RecurrenceChains (Type,Value,Disabled) VALUES (?,?,0)", recurrenceType, recurrenceValue);
                recurrenceChainId = (int)DB.Conn.LastInsertRowId();
            }

            DB.QueryAndStep(
                "UPDATE Records SET Date=?, Title=?, Amount=?, Notes=?, Account=?, RecurrenceChain=?, Automatically=0 WHERE ID=?",
                Misc.DateTimeToInt(date), name, amount, notes, accountId, recurrenceChainId, recordId);

            DB.QueryAndStep("DELETE FROM RecordsTags WHERE Record_ID=?", recordId);
            foreach (var tag in tags)
            {
                DB.QueryAndStep("INSERT INTO RecordsTags (Record_ID, Tag_ID) VALUES (?,?)", recordId, tag.ID);
            }
        }
        public bool DeleteRecord(Record record)
        {
            bool disabledRecurrence = false;
            int recordsWithRecurrenceCount, recurrenceMaxRecordId;
            using (var stmt = DB.Query("SELECT count(*) FROM Records WHERE RecurrenceChain=?", record.RecurrenceChain.ID))
            {
                stmt.Step();
                try
                {
                    recordsWithRecurrenceCount = (int)stmt.GetInteger(0);
                }
                catch (SQLiteException)
                {
                    recordsWithRecurrenceCount = 0;
                }
            }

            using (var stmt = DB.Query("SELECT max(ID) FROM Records WHERE RecurrenceChain=?", record.RecurrenceChain.ID))
            {
                stmt.Step();
                try
                {
                    recurrenceMaxRecordId = (int)stmt.GetInteger(0);
                }
                catch (SQLiteException)
                {
                    recurrenceMaxRecordId = 0;
                }
            }

            DB.QueryAndStep("DELETE FROM RecordsTags WHERE Record_ID=?", record.ID);

            DB.QueryAndStep("DELETE FROM Records WHERE ID=?", record.ID);

            if (record.RecurrenceChain.ID != 0 && recordsWithRecurrenceCount == 1)
            {
                DB.QueryAndStep("DELETE FROM RecurrenceChains WHERE ID=?", record.RecurrenceChain.ID);
                disabledRecurrence = true;
            }
            else if (recurrenceMaxRecordId == record.ID)
            {
                DisableRecurrence(record.RecurrenceChain.ID, true);
                disabledRecurrence = true;
            }

            var tagIds = new List<int>(record.Tags.Select(tag => tag.ID));
            if (tagIds.Count == 0)
                tagIds.Add(0);
            if (record.Amount < 0 && ExpensesPerTagChartMap != null)
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
                ExpensesPerTagChartMap = newTagMap;
            }
            else if (IncomePerTagChartMap != null)
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
            if (BalanceInTime != null)
            {
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
            }
            return disabledRecurrence;
        }
        public void DeleteRecordsWithAccount(int accountId)
        {
            using (ISQLiteStatement stmt = DB.Conn.Prepare(recordsSelectSQL + " WHERE Account=?"))
            {
                stmt.Bind(1, accountId);
                foreach (Record record in new RecordsEnumerator(stmt))
                {
                    DeleteRecord(record);
                }
            }
        }
        public void DisableRecurrence(int recurrenceId, bool fromDeleteRecord = false)
        {
            DB.QueryAndStep("UPDATE RecurrenceChains SET Disabled=1 WHERE ID=?", recurrenceId);
            if (!fromDeleteRecord)
                Records.Remove(Records.First(x => x.RecurrenceChain.ID == recurrenceId));
        }


        private void ClearRecords()
        {
            if (Records == null)
                Records = new ObservableCollection<Record>();
            else
                Records.Clear();
        }
        private void ClearBalanceInTime()
        {
            if (BalanceInTime == null)
                BalanceInTime = new ObservableCollection<BalanceDateChartMap>();
            else
                BalanceInTime.Clear();
        }

        #region INotifyPropertyChanged members
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
