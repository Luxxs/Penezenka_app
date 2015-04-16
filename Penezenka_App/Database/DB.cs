using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                                    FOREIGN KEY(Record_ID) REFERENCES Records(ID),
                                    FOREIGN KEY(Tag_ID) REFERENCES Tags(ID)
                                )");
            QueryAndStep(@"CREATE TABLE IF NOT EXISTS
                                Tags (
                                    ID integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                                    Title varchar(127),
                                    Color integer,
                                    Notes varchar(511),
                                    Deleted integer
                                )");

            QueryAndStep(@"CREATE TABLE IF NOT EXISTS
                                Accounts (
                                    ID integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                                    Title varchar(127),
                                    Notes varchar(511)
                                )");
            QueryAndStep("INSERT INTO Accounts (ID,Title,Notes) VALUES (0,'<žádný>','')");

            QueryAndStep(@"CREATE TABLE IF NOT EXISTS
                                RecurrenceChains (
                                    ID integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                                    Type varchar(31),
                                    Value integer,
                                    Disabled integer
                                )");
            QueryAndStep("INSERT INTO RecurrenceChains (ID,Type,Value,Disabled) VALUES (0,'',0,0)");
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
            QueryAndStep("DELETE FROM Records");
            QueryAndStep("DELETE FROM SQLITE_SEQUENCE WHERE name='Records';");
            QueryAndStep("DELETE FROM RecordsTags");
            QueryAndStep("DELETE FROM Tags");
            QueryAndStep("DELETE FROM SQLITE_SEQUENCE WHERE name='Tags';");
            QueryAndStep("DELETE FROM Accounts WHERE ID<>0");
            QueryAndStep("DELETE FROM SQLITE_SEQUENCE WHERE name='Accounts';");
            QueryAndStep("DELETE FROM RecurrenceChains WHERE ID<>0");
            QueryAndStep("DELETE FROM SQLITE_SEQUENCE WHERE name='RecurrenceChains';");
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
