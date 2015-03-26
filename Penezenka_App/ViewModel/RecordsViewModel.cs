﻿using System;
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
            ISQLiteStatement stmt = DB.conn.Prepare("SELECT ID, Date, Title, Amount, Notes, Recurrence_Type, Recurrence_Value FROM Records WHERE Date>? AND Date<?");
            stmt.Bind(1, rok*10000+mesic*100);
            stmt.Bind(2, rok*10000+(mesic+1)*100);
            ObservableCollection<Record> records = new ObservableCollection<Record>();
            List<Tag> tags;
            while(stmt.Step() == SQLiteResult.ROW)
            {
                ISQLiteStatement tagStmt = DB.conn.Prepare("SELECT ID, Title, Color, Notes FROM Tags LEFT JOIN RecordsTags ON Tag_ID=ID WHERE Record_ID=?");
                tagStmt.Bind(1,(int)stmt.GetInteger(0));
                tags = new List<Tag>();
                while (tagStmt.Step() == SQLiteResult.ROW)
                {
                    tags.Add(new Tag((int)tagStmt.GetInteger(0), tagStmt.GetText(1), (uint)tagStmt.GetInteger(2), tagStmt.GetText(3)));
                }
                records.Add(new Record(){
                                ID=(int)stmt.GetInteger(0),
                                Date=(int)stmt.GetInteger(1),
                                Title=stmt.GetText(2),
                                Amount=stmt.GetFloat(3),
                                Notes=stmt.GetText(4),
                                RecurrenceType=stmt.GetText(5),
                                RecurrenceValue=(int)stmt.GetInteger(6),
                                Tags=tags});
                tagStmt.Reset();
            }
            Records = records;
        }

        public static void InsertRecord(DateTimeOffset date, string name, double amount, string notes, List<Tag> tags, string recurrenceType, int recurrenceValue)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("BEGIN TRANSACTION");
            stmt.Step();

            stmt = DB.conn.Prepare("INSERT INTO Records (Date,Title,Amount,Notes,Recurrence_Type,Recurrence_Value) VALUES (?,?,?,?,?,?)");
            stmt.Bind(1, date.Year*10000+date.Month*100+date.Day);
            stmt.Bind(2, name);
            stmt.Bind(3, amount);
            stmt.Bind(4, notes);
            stmt.Bind(5, recurrenceType);
            stmt.Bind(6, recurrenceValue);
            //pro debugging stavu:
            SQLiteResult res = stmt.Step();
            stmt = DB.conn.Prepare("SELECT last_insert_rowid() as last_inserted_rowid");
            if (stmt.Step() == SQLiteResult.ROW)
            {
                int lastInsertedId = (int)stmt.GetInteger(0);
                foreach (var tag in tags)
                {
                    stmt = DB.conn.Prepare("INSERT INTO RecordsTags (Record_ID, Tag_ID) VALUES (?,?)");
                    stmt.Bind(1,lastInsertedId);
                    stmt.Bind(2,tag.ID);
                    stmt.Step();
                }

            }
            stmt = DB.conn.Prepare("COMMIT TRANSACTION");
            stmt.Step();
        }
        public static void UpdateRecord(int id, DateTimeOffset date, string name, double amount, string notes, List<Tag> tags, string recurrenceType, int recurrenceValue)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("BEGIN TRANSACTION");
            stmt.Step();
            
            stmt = DB.conn.Prepare("UPDATE Records SET Date=?, Title=?, Amount=?, Notes=?, Recurrence_Type=?, Recurrence_Value=? WHERE ID=?");
            stmt.Bind(1, date.Year*10000+date.Month*100+date.Day);
            stmt.Bind(2, name);
            stmt.Bind(3, amount);
            stmt.Bind(4, notes);
            stmt.Bind(5, recurrenceType);
            stmt.Bind(6, recurrenceValue);
            stmt.Bind(7, id);
            stmt.Step();
            stmt = DB.conn.Prepare("DELETE FROM RecordsTags WHERE Record_ID=?");
            stmt.Bind(1,id);
            stmt.Step();
            foreach (var tag in tags)
            {
                stmt = DB.conn.Prepare("INSERT INTO RecordsTags (Record_ID, Tag_ID) VALUES (?,?)");
                stmt.Bind(1,id);
                stmt.Bind(2,tag.ID);
                stmt.Step();
            }
            
            stmt = DB.conn.Prepare("COMMIT TRANSACTION");
            stmt.Step();
        }
        public static void DeleteRecord(int id)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("DELETE FROM Records WHERE ID=?");
            stmt.Bind(1, id);
            stmt.Step();
            stmt = DB.conn.Prepare("DELETE FROM RecordsTags WHERE Record_ID=?");
            stmt.Bind(1, id);
            stmt.Step();
        }
        public static Record GetRecord(int id)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("SELECT ID, Date, Title, Amount, Notes, Recurrence_Type, Recurrence_Value FROM Records WHERE ID=?");
            stmt.Bind(1, id);
            Record record = null;
            if(stmt.Step() == SQLiteResult.ROW)
            {
                ISQLiteStatement tagStmt = DB.conn.Prepare("SELECT * FROM RecordsTags WHERE Record_ID=?");
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
            ISQLiteStatement stmt = DB.conn.Prepare("SELECT min(Date) FROM Records");
            stmt.Step();
            return (int)stmt.GetInteger(0)/10000;
        }
        public static int GetMaxYear()
        {
            ISQLiteStatement stmt = DB.conn.Prepare("SELECT min(Date) FROM Records");
            stmt.Step();
            return (int)stmt.GetInteger(0)/10000;
        }

        public static DateTime IntToDateTime(int datum)
        {
            int rok = datum/10000;
            int mesic = datum/100 - rok*100;
            int den = datum - mesic*100 - rok*10000;
            return new DateTime(rok, mesic, den);
        }
    }
}
