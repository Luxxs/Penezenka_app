using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Penezenka_App.Database;
using SQLitePCL;

namespace Penezenka_App.Model
{
    public class Record
    {
        public int ID { get; set; }
        public int Date { get; set; }
        public string Title { get; set; }
        public double Amount { get; set; }
        public string Notes { get; set; }

        public static ObservableCollection<Record> GetMonth(int rok, int mesic)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("SELECT ID, Date, Title, Amount, Notes FROM Records WHERE Date>? AND Date<?");
            stmt.Bind(1, rok*10000+mesic*100);
            stmt.Bind(2, rok*10000+(mesic+1)*100);
            ObservableCollection<Record> records = new ObservableCollection<Record>();
            while(stmt.Step() == SQLiteResult.ROW)
            {
                records.Add(new Record(){
                                ID=(int)stmt.GetInteger(0),
                                Date=(int)stmt.GetInteger(1),
                                Title=stmt.GetText(2),
                                Amount=stmt.GetFloat(3),
                                Notes=stmt.GetText(4)});
            }
            return records;
        }
        public static void InsertRecord(DateTimeOffset date, string name, double amount, string notes)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("INSERT INTO Records (Date,Title,Amount,Notes) VALUES (?,?,?,?)");
            stmt.Bind(1, date.Year*10000+date.Month*100+date.Day);
            stmt.Bind(2, name);
            stmt.Bind(3, amount);
            stmt.Bind(4, notes);
            SQLiteResult res = stmt.Step();
        }
        public static void UpdateRecord(int id, DateTimeOffset date, string name, double amount, string notes)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("UPDATE Records SET Date=?, Title=?, Amount=?, poznamky=? WHERE ID=?");
            stmt.Bind(1, date.Year*10000+date.Month*100+date.Day);
            stmt.Bind(2, name);
            stmt.Bind(3, amount);
            stmt.Bind(4, notes);
            stmt.Bind(5, id);
            stmt.Step();
        }
        public static void DeleteRecord(int id)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("DELETE FROM Records WHERE ID=?");
            stmt.Bind(1, id);
            stmt.Step();
        }
        public static Record GetRecord(int id)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("SELECT ID, Date, Title, Amount, Notes FROM Records WHERE ID=?");
            stmt.Bind(1, id);
            Record record = null;
            if(stmt.Step() == SQLiteResult.ROW)
            {
                record = new Record() {
                                ID=(int)stmt.GetInteger(0),
                                Date=(int)stmt.GetInteger(1),
                                Title=stmt.GetText(2),
                                Amount=stmt.GetFloat(3),
                                Notes=stmt.GetText(4)};
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
