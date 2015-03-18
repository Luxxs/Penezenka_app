using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLitePCL;

namespace Penezenka_App.Model
{
    public class Polozka
    {
        public int ID { get; set; }
        public int Datum { get; set; }
        public string Nazev { get; set; }
        public double Castka { get; set; }
        public string Poznamky { get; set; }

        public static ObservableCollection<Polozka> GetMonth(int rok, int mesic)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("SELECT ID, Datum, Nazev, Castka, Poznamky FROM Polozky WHERE Datum>? AND Datum<?");
            stmt.Bind(1, rok*10000+mesic*100);
            stmt.Bind(2, rok*10000+(mesic+1)*100);
            ObservableCollection<Polozka> polozky = new ObservableCollection<Polozka>();
            while(/*polozky.Count<stmt.DataCount && */stmt.Step() == SQLiteResult.ROW)
            {
                polozky.Add(new Polozka(){
                                ID=(int)stmt.GetInteger(0),
                                Datum=(int)stmt.GetInteger(1),
                                Nazev=stmt.GetText(2),
                                Castka=stmt.GetFloat(3),
                                Poznamky=stmt.GetText(4)});
            }
            return polozky;
        }
        public static void UlozPolozku(DateTimeOffset date, string nazev, double castka, string pozn)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("INSERT INTO Polozky (Datum,Nazev,Castka,Poznamky) VALUES (?,?,?,?)");
            stmt.Bind(1, date.Year*10000+date.Month*100+date.Day);
            stmt.Bind(2, nazev);
            stmt.Bind(3, castka);
            stmt.Bind(4, pozn);
            SQLiteResult res = stmt.Step();
        }
        public static void ZmenPolozku(int id, DateTimeOffset date, string nazev, double castka, string pozn)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("UPDATE Polozky SET Datum=?, Nazev=?, castka=?, poznamky=? WHERE ID=?");
            stmt.Bind(1, date.Year*10000+date.Month*100+date.Day);
            stmt.Bind(2, nazev);
            stmt.Bind(3, castka);
            stmt.Bind(4, pozn);
            stmt.Bind(5, id);
            stmt.Step();
        }
        public static void SmazPolozku(int id)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("DELETE FROM polozky WHERE ID=?");
            stmt.Bind(1, id);
            stmt.Step();
        }
        public static Polozka GetPolozka(int id)
        {
            ISQLiteStatement stmt = DB.conn.Prepare("SELECT ID, Datum, Nazev, Castka, Poznamky FROM Polozky WHERE ID=?");
            stmt.Bind(1, id);
            Polozka polozka = null;
            if(stmt.Step() == SQLiteResult.ROW)
            {
                polozka = new Polozka() {
                                ID=(int)stmt.GetInteger(0),
                                Datum=(int)stmt.GetInteger(1),
                                Nazev=stmt.GetText(2),
                                Castka=stmt.GetFloat(3),
                                Poznamky=stmt.GetText(4)};
            }
            return polozka;
        }

        public static int GetMinYear()
        {
            ISQLiteStatement stmt = DB.conn.Prepare("SELECT min(Datum) FROM Polozky");
            stmt.Step();
            return (int)stmt.GetInteger(0)/10000;
        }
        public static int GetMaxYear()
        {
            ISQLiteStatement stmt = DB.conn.Prepare("SELECT min(Datum) FROM Polozky");
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
