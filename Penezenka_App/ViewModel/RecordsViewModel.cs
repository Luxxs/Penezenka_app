using System;
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
    class RecordsViewModel
    {
        public ObservableCollection<Record> Records { get; set; }
        
        public void GetMonth(int rok, int mesic)
        {
            ISQLiteStatement stmt = DB.Conn.Prepare(@"SELECT ID, Date, Title, Amount, Notes, Account FROM Records WHERE Date>? AND Date<?");
            stmt.Bind(1, rok*10000+mesic*100);
            stmt.Bind(2, rok*10000+(mesic+1)*100);
            ObservableCollection<Record> records = new ObservableCollection<Record>();
            List<Tag> tags;
            while(stmt.Step() == SQLiteResult.ROW)
            {
                int recordID = (int) stmt.GetInteger(0);
                ISQLiteStatement tagStmt = DB.Conn.Prepare("SELECT ID, Title, Color, Notes FROM Tags LEFT JOIN RecordsTags ON Tag_ID=ID WHERE Record_ID=?");
                tagStmt.Bind(1,(int)stmt.GetInteger(0));
                tags = new List<Tag>();
                while (tagStmt.Step() == SQLiteResult.ROW)
                {
                    tags.Add(new Tag((int)tagStmt.GetInteger(0), tagStmt.GetText(1), (uint)tagStmt.GetInteger(2), tagStmt.GetText(3)));
                }
                var recStmt = DB.Conn.Prepare(@"SELECT ID,Type, Value FROM RecordsRecurrenceChains
                                                JOIN RecurrenceChains ON RecurrenceChain_ID=ID
                                                WHERE Record_ID=? AND Disabled<>1");
                recStmt.Bind(1,recordID);
                recStmt.Step();
                RecurrenceChain recurrenceChain = null;
                try
                {
                    recurrenceChain = new RecurrenceChain
                    {
                        ID = (int)recStmt.GetInteger(0),
                        Type = recStmt.GetText(1),
                        Value = (int)recStmt.GetInteger(2)
                    };
                }
                catch (SQLiteException ex) { }

                int accountId = (int)stmt.GetInteger(5);
                Account account;
                if (accountId != 0)
                {
                    var accStmt = DB.Conn.Prepare("SELECT Title, Notes FROM Accounts WHERE ID=?");
                    accStmt.Bind(1,accountId);
                    accStmt.Step();
                    account = new Account(){
                                    ID=accountId,
                                    Title=accStmt.GetText(0),
                                    Notes=accStmt.GetText(1)};
                }
                else
                {
                    account = new Account(){ID=0,Title="<žádný>",Notes=null};
                }

                records.Add(new Record(){
                                ID=recordID,
                                Date=(int)stmt.GetInteger(1),
                                Title=stmt.GetText(2),
                                Amount=stmt.GetFloat(3),
                                Notes=stmt.GetText(4),
                                Account=account,
                                Tags=tags,
                                RecurrenceChain=recurrenceChain});
                tagStmt.Reset();
            }
            Records = records;
        }
        /*public static Record GetRecord(int id)
        {
            ISQLiteStatement stmt = DB.Conn.Prepare("SELECT ID, Date, Title, Amount, Notes, Recurrence_Type, Recurrence_Value FROM Records WHERE ID=?");
            stmt.Bind(1, id);
            Record record = null;
            if(stmt.Step() == SQLiteResult.ROW)
            {
                ISQLiteStatement tagStmt = DB.Conn.Prepare("SELECT * FROM RecordsTags WHERE Record_ID=?");
                var tags = new List<Tag>();
                while (tagStmt.Step() == SQLiteResult.ROW)
                {
                    tags.Add(new Tag((int)tagStmt.GetInteger(0), tagStmt.GetText(1), (uint)tagStmt.GetInteger(2), tagStmt.GetText(3)));
                }
                record = new Record() {
                                ID=(int)stmt.GetInteger(0),
                                Date=(int)stmt.GetInteger(1),
                                Title=stmt.GetText(2),
                                Amount=stmt.GetFloat(3),
                                Notes=stmt.GetText(4),
                                RecurrenceType=stmt.GetText(5),
                                RecurrenceValue=(int)stmt.GetInteger(6),
                                Tags=tags};
            }
            return record;
        }*/

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

        /* INSERT, UPDATE, DELETE */
        public static void InsertRecord(int accountId, DateTimeOffset date, string name, double amount, string notes, List<Tag> tags, string recurrenceType, int recurrenceValue)
        {
            var stmt = DB.Conn.Prepare("PRAGMA foreign_keys=OFF");
            stmt.Step();
            stmt = DB.Conn.Prepare("INSERT INTO Records (Date,Title,Amount,Notes,Account) VALUES (?,?,?,?,?)");
            stmt.Bind(1, DateTimeToInt(date));
            stmt.Bind(2, name);
            stmt.Bind(3, amount);
            stmt.Bind(4, notes);
            stmt.Bind(5, accountId);
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

            int recurrenceChainID = 0;
            if (recurrenceType != null)
            {
                stmt = DB.Conn.Prepare("INSERT INTO RecurrenceChains (Type,Value,Disabled) VALUES (?,?,0)");
                stmt.Bind(1,recurrenceType);
                stmt.Bind(2,recurrenceValue);
                stmt.Step();
                stmt.Reset();
                recurrenceChainID = (int)DB.Conn.LastInsertRowId();
                stmt = DB.Conn.Prepare("INSERT INTO RecordsRecurrenceChains (Record_ID,RecurrenceChain_ID) VALUES (?,?)");
                stmt.Bind(1,recordID);
                stmt.Bind(2,recurrenceChainID);
                stmt.Step();
            }
            
        }
        public static void UpdateRecord(int id, int accountId, DateTimeOffset date, string name, double amount, string notes,
            List<Tag> tags, string recurrenceType, int recurrenceValue)
        {
            ISQLiteStatement stmt = DB.Conn.Prepare("BEGIN TRANSACTION");
            stmt.Step();

            int recurrenceId = 0;
            try
            {
                stmt = DB.Conn.Prepare(@"SELECT * FROM RecordsRecurrenceChains WHERE Record_ID=?");
                SQLiteResult res = stmt.Step();
                recurrenceId = (int) stmt.GetInteger(0);
                //pokud starý je a nový není
                if (recurrenceType == null)
                {
                    stmt = DB.Conn.Prepare(("UPDATE RecurrenceChains SET Disabled=1 WHERE ID=?"));
                    stmt.Bind(1, recurrenceId);
                    stmt.Step();
                }
                else
                {
                    stmt = DB.Conn.Prepare(("UPDATE RecurrenceChains SET Type=?, Value=?, Disabled=0 WHERE ID=?"));
                    stmt.Bind(1, recurrenceType);
                    stmt.Bind(2, recurrenceValue);
                    stmt.Bind(3, recurrenceId);
                    stmt.Step();
                }
            }
            catch (SQLiteException ex)
            {
                //todo: přidat vložení fixnosti výdaje
                stmt = DB.Conn.Prepare("INSERT INTO RecurrenceChains (Type,Value,Disabled) VALUES (?,?,0)");
                stmt.Bind(1,recurrenceType);
                stmt.Bind(2,recurrenceValue);
                stmt.Step();
                stmt.Reset();
                recurrenceId = (int)DB.Conn.LastInsertRowId();
                stmt = DB.Conn.Prepare("INSERT INTO RecordsRecurrenceChains (Record_ID,RecurrenceChain_ID) VALUES(?,?)");
                stmt.Bind(1,id);
                stmt.Bind(2,recurrenceId);
                stmt.Step();
            }
            
            stmt = DB.Conn.Prepare("PRAGMA foreign_keys=OFF");
            stmt.Step();
            stmt = DB.Conn.Prepare("UPDATE Records SET Date=?, Title=?, Amount=?, Notes=?, Account=? WHERE ID=?");
            stmt.Bind(1, date.Year*10000+date.Month*100+date.Day);
            stmt.Bind(2, name);
            stmt.Bind(3, amount);
            stmt.Bind(4, notes);
            stmt.Bind(5, accountId);
            stmt.Bind(6, id);
            stmt.Step();
            stmt = DB.Conn.Prepare("PRAGMA foreign_keys=ON");
            stmt.Step();
            //dalo by se vyhledávat, která přiřazení štítků k záznamům se mají odstranit, ale zde to asi nemá smysl.
            stmt = DB.Conn.Prepare("DELETE FROM RecordsTags WHERE Record_ID=?");
            stmt.Bind(1,id);
            stmt.Step();
            foreach (var tag in tags)
            {
                stmt = DB.Conn.Prepare("INSERT INTO RecordsTags (Record_ID, Tag_ID) VALUES (?,?)");
                stmt.Bind(1,id);
                stmt.Bind(2,tag.ID);
                stmt.Step();
            }
            
            stmt = DB.Conn.Prepare("COMMIT TRANSACTION");
            stmt.Step();
        }
        public static void DeleteRecord(int recordId, bool disableRecurrence)
        {
            ISQLiteStatement stmt = DB.Conn.Prepare("BEGIN TRANSACTION");
            stmt.Step();
            
            stmt = DB.Conn.Prepare("DELETE FROM Records WHERE ID=?");
            stmt.Bind(1, recordId);
            stmt.Step();
            stmt = DB.Conn.Prepare("DELETE FROM RecordsTags WHERE Record_ID=?");
            stmt.Bind(1, recordId);
            stmt.Step();
            //todo: asi bych to dal vždycky (při smazání fixního výdaje zrušit i jeho další opakované přidávání) - ale zobrazit upozornění na tohle↓
            if (disableRecurrence)
            {
                int recordsWithRecurrcenceCount = 0;
                try
                {
                    stmt = DB.Conn.Prepare("SELECT count(Record_ID) FROM RecordsRecurrenceChains WHERE Record_ID=?");
                    stmt.Bind(1, recordId);
                    stmt.Step();
                    recordsWithRecurrcenceCount = (int) stmt.GetInteger(0);
                    stmt = DB.Conn.Prepare("SELECT RecurrenceChain_ID FROM RecordsRecurrenceChains WHERE Record_ID=?");
                    stmt.Bind(1,recordId);
                    stmt.Step();
                    int recurrenceChainId = (int)stmt.GetInteger(0);
                    if (recordsWithRecurrcenceCount == 1)
                    {
                        stmt = DB.Conn.Prepare("DELETE FROM RecordsRecurrenceChains WHERE Record_ID=?");
                        stmt.Bind(1, recordId);
                        stmt.Step();
                        stmt = DB.Conn.Prepare("DELETE FROM RecurrenceChains WHERE ID=?");
                        stmt.Bind(1, recurrenceChainId);
                        stmt.Step();
                    }
                    else
                    {
                        stmt = DB.Conn.Prepare("UPDATE RecurrenceChains SET Disabled=1 WHERE ID=?");
                        stmt.Bind(1, recurrenceChainId);
                        stmt.Step();
                    }
                }
                catch (SQLiteException) { }
            }
            
            stmt = DB.Conn.Prepare("COMMIT TRANSACTION");
            stmt.Step();
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
