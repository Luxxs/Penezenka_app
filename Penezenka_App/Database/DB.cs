using System;
using System.Collections.Generic;
using Penezenka_App.Model;
using Penezenka_App.OtherClasses;
using Penezenka_App.ViewModel;
using SQLitePCL;

namespace Penezenka_App.Database
{
    static class DB
    {
        public static SQLiteConnection Conn;
        
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
                QueryAndStep("INSERT INTO Accounts (ID,Title,Notes) VALUES (0,'','')");
                QueryAndStep("INSERT INTO RecurrenceChains (ID,Type,Value,Disabled) VALUES (0,'',0,1)");
            }
        }

        public static void AddRecurrentRecords()
        {
            RecordsViewModel recordsViewModel = new RecordsViewModel();
            recordsViewModel.GetRecurrentRecords();
            foreach (var record in recordsViewModel.Records)
            {
                QueryAndStep(
                    "INSERT INTO Records (Date,Title,Amount,Notes,Account,RecurrenceChain,Automatically) VALUES (?,?,?,?,?,?,1)",
                    Misc.DateTimeToInt(record.Date), record.Title, record.Amount, record.Notes,
                    record.Account.ID, record.RecurrenceChain.ID);
                int lastInsertRecordId = (int)Conn.LastInsertRowId();
                foreach (var tag in record.Tags)
                {
                    QueryAndStep("INSERT INTO RecordsTags (Record_ID,Tag_ID) VALUES (?,?)", lastInsertRecordId, tag.ID);
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
            using (var stmt = Query("SELECT ID, Title, Notes FROM Accounts WHERE ID <> 0"))
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
            using (var stmt = Query("SELECT ID, Type, Value, Disabled FROM RecurrenceChains WHERE ID<>0"))
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
            using (var stmt = Query("SELECT ID, Title, Color, Notes FROM Tags"))
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
                    Query(
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
            using (var stmt = Query("SELECT Record_ID, Tag_ID FROM RecordsTags"))
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
            int maxId = 0;
            if (exportData.Accounts.Count > 0)
            {
                foreach (var account in exportData.Accounts)
                {
                    QueryAndStep("INSERT INTO Accounts (ID, Title, Notes) VALUES (?,?,?)", account.ID, account.Title,
                        account.Notes);
                    if (account.ID > maxId)
                        maxId = account.ID;
                }
                QueryAndStep("UPDATE SQLITE_SEQUENCE SET seq = ? WHERE name='Accounts'", maxId + 1);
                maxId = 0;
            }

            if (exportData.RecurrenceChains.Count > 0)
            {
                foreach (var recurrenceChain in exportData.RecurrenceChains)
                {
                    QueryAndStep("INSERT INTO RecurrenceChains (ID, Type, Value, Disabled) VALUES (?,?,?,?)",
                        recurrenceChain.ID, recurrenceChain.Type, recurrenceChain.Value,
                        Convert.ToInt32(recurrenceChain.Disabled));
                    if (recurrenceChain.ID > maxId)
                        maxId = recurrenceChain.ID;
                }
                QueryAndStep("UPDATE SQLITE_SEQUENCE SET seq = ? WHERE name='RecurrenceChains'", maxId + 1);
                maxId = 0;
            }

            if (exportData.Tags.Count > 0)
            {
                foreach (var tagForExport in exportData.Tags)
                {
                    QueryAndStep("INSERT INTO Tags (ID, Title, Color, Notes) VALUES (?,?,?,?)", tagForExport.ID,
                        tagForExport.Title, tagForExport.Color, tagForExport.Notes);
                    if (tagForExport.ID > maxId)
                        maxId = tagForExport.ID;
                }
                QueryAndStep("UPDATE SQLITE_SEQUENCE SET seq = ? WHERE name='Tags'", maxId + 1);
                maxId = 0;
            }

            if (exportData.Records.Count > 0)
            {
                foreach (var recordForExport in exportData.Records)
                {
                    QueryAndStep(
                        "INSERT INTO Records(ID, Date,Title,Amount,Notes,Account,RecurrenceChain,Automatically) VALUES (?,?,?,?,?,?,?,?)",
                        recordForExport.ID, recordForExport.Date, recordForExport.Title, recordForExport.Amount,
                        recordForExport.Notes, recordForExport.AccountID, recordForExport.RecurrenceChainID,
                        Convert.ToInt32(recordForExport.Automatically));
                    if (recordForExport.ID > maxId)
                        maxId = recordForExport.ID;
                }
                QueryAndStep("UPDATE SQLITE_SEQUENCE SET seq = ? WHERE name='Records'", maxId + 1);
            }

            foreach (var recordTagForExport in exportData.RecordsTags)
            {
                QueryAndStep("INSERT INTO RecordsTags (Record_ID, Tag_ID) VALUES (?,?)", recordTagForExport.Record_ID, recordTagForExport.Tag_ID);
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
            using (ISQLiteStatement stmt = Query(query, bindings))
            {
                SQLiteResult res = stmt.Step();
                return res;
            }
        }
    }
}
