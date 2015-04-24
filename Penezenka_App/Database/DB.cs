using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Penezenka_App.Model;
using Penezenka_App.OtherClasses;
using Penezenka_App.ViewModel;
using SQLitePCL;

namespace Penezenka_App.Database
{
    static class DB
    {
        public static SQLiteConnection Conn = null;
        
        public static void PrepareDatabase()
        {
            Conn = new SQLiteConnection("databaze.db");
            QueryAndStep("PRAGMA foreign_keys = ON");
            QueryAndStep(@"CREATE TABLE IF NOT EXISTS
                                Records (
                                    ID integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                                    Date integer,
                                    Title varchar(255),
                                    Amount float,
                                    Notes varchar(511),
                                    Account integer,
                                    RecurrenceChain integer,
                                    Automatically integer DEFAULT 0,
                                    FOREIGN KEY(Account) REFERENCES Accounts(ID),
                                    FOREIGN KEY(RecurrenceChain) REFERENCES RecurrenceChains(ID)
                                )");
            QueryAndStep("CREATE INDEX IF NOT EXISTS records_id_idx ON Records(ID)");
            QueryAndStep("CREATE INDEX IF NOT EXISTS records_date_idx ON Records(Date)");
            QueryAndStep(@"CREATE TABLE IF NOT EXISTS
                                RecordsTags (
                                    Record_ID integer,
                                    Tag_ID integer,
                                    PRIMARY KEY (Record_ID, Tag_ID),
                                    FOREIGN KEY(Record_ID) REFERENCES Records(ID),
                                    FOREIGN KEY(Tag_ID) REFERENCES Tags(ID)
                                )");
            QueryAndStep(@"CREATE TABLE IF NOT EXISTS
                                Tags (
                                    ID integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                                    Title varchar(127),
                                    Color integer,
                                    Notes varchar(511)
                                )");

            QueryAndStep(@"CREATE TABLE IF NOT EXISTS
                                Accounts (
                                    ID integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                                    Title varchar(127),
                                    Notes varchar(511)
                                )");

            QueryAndStep(@"CREATE TABLE IF NOT EXISTS
                                RecurrenceChains (
                                    ID integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                                    Type varchar(2),
                                    Value integer,
                                    Disabled integer
                                )");
            if (QueryAndStep("SELECT * FROM Accounts") == SQLiteResult.DONE)
            {
                QueryAndStep("INSERT INTO Accounts (ID,Title,Notes) VALUES (0,'<žádný>','')");
                QueryAndStep("INSERT INTO RecurrenceChains (ID,Type,Value,Disabled) VALUES (0,'',0,0)");
            }
        }

        public static void AddRecurrentRecords()
        {
            RecordsViewModel recordsViewModel = new RecordsViewModel();
            recordsViewModel.GetRecurrentRecords(false);
            ISQLiteStatement stmt;
            foreach (var record in recordsViewModel.Records)
            {
                stmt = Conn.Prepare("INSERT INTO Records (Date,Title,Amount,Notes,Account,RecurrenceChain,Automatically) VALUES (?,?,?,?,?,?,1)");
                stmt.Bind(1,RecordsViewModel.DateTimeToInt(record.Date));
                stmt.Bind(2,record.Title);
                stmt.Bind(3,record.Amount);
                stmt.Bind(4,record.Notes);
                stmt.Bind(5,record.Account.ID);
                stmt.Bind(6,record.RecurrenceChain.ID);
                stmt.Step();
                stmt.Reset();
                int lastInsertRecordId = (int)Conn.LastInsertRowId();
                foreach (var tag in record.Tags)
                {
                    stmt = Conn.Prepare("INSERT INTO RecordsTags (Record_ID,Tag_ID) VALUES (?,?)");
                    stmt.Bind(1,lastInsertRecordId);
                    stmt.Bind(2,tag.ID);
                    stmt.Step();
                }
            }
        }

        public static void ClearTables()
        {
            QueryAndStep("BEGIN TRANSACTION");
            QueryAndStep("DELETE FROM RecordsTags");
            QueryAndStep("DELETE FROM Records");
            QueryAndStep("DELETE FROM SQLITE_SEQUENCE WHERE name='Records'");
            QueryAndStep("DELETE FROM Tags");
            QueryAndStep("DELETE FROM SQLITE_SEQUENCE WHERE name='Tags'");
            QueryAndStep("DELETE FROM Accounts WHERE ID<>0");
            QueryAndStep("DELETE FROM SQLITE_SEQUENCE WHERE name='Accounts'");
            QueryAndStep("DELETE FROM RecurrenceChains WHERE ID<>0");
            QueryAndStep("DELETE FROM SQLITE_SEQUENCE WHERE name='RecurrenceChains'");
            QueryAndStep("COMMIT TRANSACTION");
        }

        public static ExportData GetExportData()
        {
            var accounts = new List<Account>();
            using (var stmt = Conn.Prepare("SELECT ID, Title, Notes FROM Accounts WHERE ID <> 0"))
            {
                while (stmt.Step() == SQLiteResult.ROW)
                {
                    accounts.Add(new Account
                    {
                        ID = (int) stmt.GetInteger(0),
                        Title = stmt.GetText(1),
                        Notes = stmt.GetText(2)
                    });
                }
            }
            var recurrenceChains = new List<RecurrenceChain>();
            using (var stmt = Conn.Prepare("SELECT ID, Type, Value, Disabled FROM RecurrenceChains WHERE ID<>0"))
            {
                while (stmt.Step() == SQLiteResult.ROW)
                {
                    recurrenceChains.Add(new RecurrenceChain
                    {
                        ID = (int) stmt.GetInteger(0),
                        Type = stmt.GetText(1),
                        Value = (int) stmt.GetInteger(2),
                        Disabled = Convert.ToBoolean(stmt.GetInteger(3))
                    });
                }
            }
            var tags = new List<TagForExport>();
            using (var stmt = Conn.Prepare("SELECT ID, Title, Color, Notes FROM Tags"))
            {
                while (stmt.Step() == SQLiteResult.ROW)
                {
                    tags.Add(new TagForExport
                    {
                        ID = (int) stmt.GetInteger(0),
                        Title = stmt.GetText(1),
                        Color = (uint) stmt.GetInteger(2),
                        Notes = stmt.GetText(3)
                    });
                }
            }
            var records = new List<RecordForExport>();
            using (
                var stmt =
                    Conn.Prepare(
                        "SELECT ID, Date, Title, Amount, Notes, Account, RecurrenceChain, Automatically FROM Records"))
            {
                while (stmt.Step() == SQLiteResult.ROW)
                {
                    records.Add(new RecordForExport
                    {
                        ID = (int) stmt.GetInteger(0),
                        Date = (int) stmt.GetInteger(1),
                        Title = stmt.GetText(2),
                        Amount = stmt.GetFloat(3),
                        Notes = stmt.GetText(4),
                        AccountID = (int) stmt.GetInteger(5),
                        RecurrenceChainID = (int) stmt.GetInteger(6),
                        Automatically = Convert.ToBoolean(stmt.GetInteger(7))
                    });
                }
            }
            var recordsTags = new List<RecordTagForExport>();
            using (var stmt = Conn.Prepare("SELECT Record_ID, Tag_ID FROM RecordsTags"))
            {
                while (stmt.Step() == SQLiteResult.ROW)
                {
                    recordsTags.Add(new RecordTagForExport
                    {
                        Record_ID = (int) stmt.GetInteger(0),
                        Tag_ID = (int) stmt.GetInteger(1)
                    });
                }
            }
            return new ExportData
            {
                Accounts = accounts,
                RecurrenceChains = recurrenceChains,
                Tags = tags,
                Records = records,
                RecordsTags = recordsTags
            };
        }

        public static void SaveDataFromExport(ExportData exportData)
        {
            ClearTables();
            ISQLiteStatement stmt = null;
            int maxId = 0;
            foreach (var account in exportData.Accounts)
            {
                stmt = Conn.Prepare("INSERT INTO Accounts (ID, Title, Notes) VALUES (?,?,?)");
                stmt.Bind(1, account.ID);
                stmt.Bind(2, account.Title);
                stmt.Bind(3, account.Notes);
                stmt.Step();
                if (account.ID > maxId)
                    maxId = account.ID;
            }
            if (exportData.Accounts.Count > 0)
            {
                stmt = Conn.Prepare("UPDATE SQLITE_SEQUENCE SET seq = ? WHERE name='Accounts'");
                stmt.Bind(1, maxId+1);
                stmt.Step();
                maxId = 0;
            }

            foreach (var recurrenceChain in exportData.RecurrenceChains)
            {
                stmt = Conn.Prepare("INSERT INTO RecurrenceChains (ID, Type, Value, Disabled) VALUES (?,?,?,?)");
                stmt.Bind(1, recurrenceChain.ID);
                stmt.Bind(2, recurrenceChain.Type);
                stmt.Bind(3, recurrenceChain.Value);
                stmt.Bind(4, Convert.ToInt32(recurrenceChain.Disabled));
                stmt.Step();
                if (recurrenceChain.ID > maxId)
                    maxId = recurrenceChain.ID;
            }
            if (exportData.RecurrenceChains.Count > 0)
            {
                stmt = Conn.Prepare("UPDATE SQLITE_SEQUENCE SET seq = ? WHERE name='RecurrenceChains'");
                stmt.Bind(1, maxId+1);
                stmt.Step();
                maxId = 0;
            }

            foreach (var tagForExport in exportData.Tags)
            {
                stmt = Conn.Prepare("INSERT INTO Tags (ID, Title, Color, Notes) VALUES (?,?,?,?)");
                stmt.Bind(1, tagForExport.ID);
                stmt.Bind(2, tagForExport.Title);
                stmt.Bind(3, tagForExport.Color);
                stmt.Bind(4, tagForExport.Notes);
                stmt.Step();
                if (tagForExport.ID > maxId)
                    maxId = tagForExport.ID;
            }
            if (exportData.Tags.Count > 0)
            {
                stmt = Conn.Prepare("UPDATE SQLITE_SEQUENCE SET seq = ? WHERE name='Tags'");
                stmt.Bind(1, maxId+1);
                stmt.Step();
                maxId = 0;
            }
            if(stmt != null)
                stmt.Reset();

            foreach (var recordForExport in exportData.Records)
            {
                stmt =
                    Conn.Prepare(
                        "INSERT INTO Records(ID, Date,Title,Amount,Notes,Account,RecurrenceChain,Automatically) VALUES (?,?,?,?,?,?,?,?)");
                stmt.Bind(1, recordForExport.ID);
                stmt.Bind(2, recordForExport.Date);
                stmt.Bind(3, recordForExport.Title);
                stmt.Bind(4, recordForExport.Amount);
                stmt.Bind(5, recordForExport.Notes);
                stmt.Bind(6, recordForExport.AccountID);
                stmt.Bind(7, recordForExport.RecurrenceChainID);
                stmt.Bind(8, Convert.ToInt32(recordForExport.Automatically));
                stmt.Step();
                if (recordForExport.ID > maxId)
                    maxId = recordForExport.ID;
            }
            if (exportData.Records.Count > 0)
            {
                stmt = Conn.Prepare("UPDATE SQLITE_SEQUENCE SET seq = ? WHERE name='Records'");
                stmt.Bind(1, maxId+1);
                stmt.Step();
            }
            if(stmt != null)
                stmt.Reset();

            foreach (var recordTagForExport in exportData.RecordsTags)
            {
                stmt = Conn.Prepare("INSERT INTO RecordsTags (Record_ID, Tag_ID) VALUES (?,?)");
                stmt.Bind(1, recordTagForExport.Record_ID);
                stmt.Bind(2, recordTagForExport.Tag_ID);
                stmt.Step();
            }
        }

        public static ISQLiteStatement Query(string query, params object[] bindings)
        {
            ISQLiteStatement stmt = Conn.Prepare(query);
            for (int i = 0; i < bindings.Length; i++)
            {
                stmt.Bind(i+1, bindings[i]);
            }
            return stmt;
        }
        public static SQLiteResult QueryAndStep(string query, params object[] bindings)
        {
            ISQLiteStatement stmt = Query(query, bindings);
            SQLiteResult res = stmt.Step();
            return res;
        }
    }
}
