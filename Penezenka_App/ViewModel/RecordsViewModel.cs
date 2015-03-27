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
            ISQLiteStatement stmt = DB.Conn.Prepare("SELECT ID, Date, Title, Amount, Notes FROM Records WHERE Date>? AND Date<?");
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
                var recStmt = DB.Conn.Prepare(@"SELECT Type, Value FROM RecordsRecurrenceChains
                                                JOIN RecurrenceChains ON RecurrenceChain_ID=ID
                                                WHERE Record_ID=? AND Disabled<>1");
                recStmt.Bind(1,recordID);
                recStmt.Step();
                string recurrType = null;
                int recurrValue = 0;
                try
                {
                    recurrType = recStmt.GetText(0);
                    recurrValue = (int)recStmt.GetInteger(1);
                }
                catch (SQLiteException ex) { }

                records.Add(new Record(){
                                ID=recordID,
                                Date=(int)stmt.GetInteger(1),
                                Title=stmt.GetText(2),
                                Amount=stmt.GetFloat(3),
                                Notes=stmt.GetText(4),
                                RecurrenceType=recurrType,
                                RecurrenceValue=recurrValue,
                                Tags=tags});
                tagStmt.Reset();
            }
            Records = records;
        }
        public static Record GetRecord(int id)
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

        /* INSERT, UPDATE, DELETE */
        public static void InsertRecord(DateTimeOffset date, string name, double amount, string notes, List<Tag> tags, string recurrenceType, int recurrenceValue)
        {
            ISQLiteStatement stmt = DB.Conn.Prepare("BEGIN TRANSACTION");
            stmt.Step();

            int recurrenceChainID = 0;
            if (recurrenceType != null)
            {
                stmt = DB.Conn.Prepare("INSERT INTO RecurrenceChains (Type,Value,Disabled) VALUES (?,?,0)");
                stmt.Bind(1,recurrenceType);
                stmt.Bind(2,recurrenceValue);
                stmt.Step();
                stmt = DB.Conn.Prepare("SELECT last_insert_rowid() as last_inserted_rowid");
                stmt.Step();
                recurrenceChainID = (int)stmt.GetInteger(0);
            }

            stmt = DB.Conn.Prepare("INSERT INTO Records (Date,Title,Amount,Notes) VALUES (?,?,?,?)");
            stmt.Bind(1, DateTimeToInt(date));
            stmt.Bind(2, name);
            stmt.Bind(3, amount);
            stmt.Bind(4, notes);
            //proměnná res je tu pro debugging stavu:
            SQLiteResult res = stmt.Step();
            int recordID = 0;
            stmt = DB.Conn.Prepare("SELECT last_insert_rowid() as last_inserted_rowid");
            if (stmt.Step() == SQLiteResult.ROW)
            {
                recordID = (int)stmt.GetInteger(0);
                foreach (var tag in tags)
                {
                    stmt = DB.Conn.Prepare("INSERT INTO RecordsTags (Record_ID, Tag_ID) VALUES (?,?)");
                    stmt.Bind(1,recordID);
                    stmt.Bind(2,tag.ID);
                    stmt.Step();
                }

            }

            if (recurrenceType != null)
            {
                stmt = DB.Conn.Prepare("INSERT INTO RecordsRecurrenceChains (Record_ID,RecurrenceChain_ID) VALUES (?,?)");
                stmt.Bind(1,recurrenceChainID);
                stmt.Bind(2,recordID);
                stmt.Step();
            }
            stmt = DB.Conn.Prepare("COMMIT TRANSACTION");
            stmt.Step();
        }
        public static void UpdateRecord(int id, DateTimeOffset date, string name, double amount, string notes, List<Tag> tags, string recurrenceType, int recurrenceValue)
        {
            ISQLiteStatement stmt = DB.Conn.Prepare("BEGIN TRANSACTION");
            stmt.Step();

            try
            {
                int recurrenceID = 0;
                stmt = DB.Conn.Prepare(@"SELECT * FROM RecordsRecurrenceChains WHERE Record_ID=?");
                SQLiteResult res = stmt.Step();
                recurrenceID = (int) stmt.GetInteger(0);
                if (recurrenceType == null)
                {
                    stmt = DB.Conn.Prepare(("UPDATE RecurrenceChains SET Disabled=1 WHERE ID=?"));
                    stmt.Bind(1, recurrenceID);
                    stmt.Step();
                }
                else
                {
                    stmt = DB.Conn.Prepare(("UPDATE RecurrenceChains SET Type=?, Value=?, Disabled=0 WHERE ID=?"));
                    stmt.Bind(1,recurrenceType);
                    stmt.Bind(2,recurrenceValue);
                    stmt.Bind(3,recurrenceID);
                    stmt.Step();
                }
            }
            catch (SQLiteException ex) { }

            stmt = DB.Conn.Prepare("UPDATE Records SET Date=?, Title=?, Amount=?, Notes=? WHERE ID=?");
            stmt.Bind(1, date.Year*10000+date.Month*100+date.Day);
            stmt.Bind(2, name);
            stmt.Bind(3, amount);
            stmt.Bind(4, notes);
            stmt.Bind(5, id);
            stmt.Step();
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
        public static void DeleteRecord(int id, bool disableRecurrence)
        {
            ISQLiteStatement stmt = DB.Conn.Prepare("DELETE FROM Records WHERE ID=?");
            stmt.Bind(1, id);
            stmt.Step();
            stmt = DB.Conn.Prepare("DELETE FROM RecordsTags WHERE Record_ID=?");
            stmt.Bind(1, id);
            stmt.Step();
            //todo: asi bych to dal vždycky - zobrazit upozornění na tohle↓
            if (disableRecurrence)
            {
                stmt = DB.Conn.Prepare("SELECT RecurrenceChain_ID FROM RecordsRecurrenceChains WHERE Record_ID=?");
                stmt.Bind(1,id);
                stmt.Step();
                int recChainID = (int)stmt.GetInteger(0);
                stmt = DB.Conn.Prepare("UPDATE RecurrenceChains SET Disabled=1 WHERE ID=?");
                stmt.Bind(1, recChainID);
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
