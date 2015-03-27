using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Penezenka_App.Database;
using Penezenka_App.Model;
using Penezenka_App.OtherClasses;
using SQLitePCL;

namespace Penezenka_App.ViewModel
{
    class TagViewModel
    {
        public ObservableCollection<Tag> Tags { get; set; }
        
        public void GetTags()
        {
            var stmt = DB.Conn.Prepare(@"SELECT ID, Title, Color, Notes, count(Record_ID)
                                         FROM Tags
                                         LEFT JOIN RecordsTags ON Tag_ID=ID
                                         GROUP BY ID");
            ObservableCollection<Tag> tags = new ObservableCollection<Tag>();
            while(stmt.Step() == SQLiteResult.ROW)
            {
                tags.Add(new Tag((int)stmt.GetInteger(0), stmt.GetText(1), (uint)stmt.GetInteger(2), stmt.GetText(3), (int)stmt.GetInteger(4)));
            }
            Tags = tags;
        }
        public static void InsertTag(string name, Color color, string notes)
        {
            ISQLiteStatement stmt = DB.Conn.Prepare("INSERT INTO Tags (Title,Color,Notes) VALUES (?,?,?)");
            stmt.Bind(1, name);
            stmt.Bind(2, MyColors.ColorToUInt(color));
            stmt.Bind(3, notes);
            SQLiteResult res = stmt.Step();
        }
        public static void UpdateTag(int id, string name, Color color, string notes)
        {
            ISQLiteStatement stmt = DB.Conn.Prepare("UPDATE Tags SET Title=?, Color=?, Notes=? WHERE ID=?");
            stmt.Bind(1, name);
            stmt.Bind(2, MyColors.ColorToUInt(color));
            stmt.Bind(3, notes);
            stmt.Bind(4, id);
            stmt.Step();
        }
        public static void DeleteTag(int id)
        {
            ISQLiteStatement stmt = DB.Conn.Prepare("BEGIN TRANSACTION");
            stmt.Step();

            stmt = DB.Conn.Prepare("DELETE FROM Tags WHERE ID=?");
            stmt.Bind(1, id);
            stmt.Step();
            stmt = DB.Conn.Prepare("DELETE FROM RecordsTags WHERE Tag_ID=?");
            stmt.Bind(1, id);
            stmt.Step();
             
            stmt = DB.Conn.Prepare("COMMIT TRANSACTION");
            stmt.Step();
        }
        public static Tag GetTag(int id)
        {
            ISQLiteStatement stmt = DB.Conn.Prepare("SELECT ID, Title, Color, Notes FROM Tags WHERE ID=?");
            stmt.Bind(1, id);
            Tag tag = null;
            if(stmt.Step() == SQLiteResult.ROW)
            {
                tag = new Tag((int)stmt.GetInteger(0), stmt.GetText(0), (uint)stmt.GetInteger(0), stmt.GetText(0));
            }
            return tag;
        }
    }
}
