using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLitePCL;

namespace Penezenka_App.Model
{
    class DB
    {
        public static SQLiteConnection conn = null;
        
        public static void PrepareDatabase()
        {
            conn = new SQLiteConnection("databaze.db");
            bezParamDotaz("PRAGMA foreign_keys = ON");
            bezParamDotaz(@"CREATE TABLE IF NOT EXISTS
                                Polozky (
                                    ID integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                                    Datum integer,
                                    Nazev varchar(255),
                                    Castka float,
                                    Poznamky varchar(511))");
            bezParamDotaz("CREATE INDEX IF NOT EXISTS polozky_datum_idx ON Polozky(Datum)");
            bezParamDotaz(@"CREATE TABLE IF NOT EXISTS
                                StitkyKPolozkam (
                                    ID_polozky integer,
                                    ID_stitku integer,
                                    FOREIGN KEY(ID_polozky) REFERENCES Polozky(ID),
                                    FOREIGN KEY(ID_stitku) REFERENCES Stitky(ID))");
            bezParamDotaz(@"CREATE TABLE IF NOT EXISTS
                                Stitky (
                                    ID integer PRIMARY KEY AUTOINCREMENT NOT NULL,
                                    Nazev varchar(127),
                                    ARGB character(9))");
        }
        public static void bezParamDotaz(string dotaz)
        {
            conn.Prepare(dotaz).Step();
        }
    }
}
