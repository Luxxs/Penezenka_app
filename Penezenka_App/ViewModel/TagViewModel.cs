using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;
using Penezenka_App.Database;
using Penezenka_App.Model;
using Penezenka_App.OtherClasses;
using SQLitePCL;

namespace Penezenka_App.ViewModel
{
    class TagViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Tag> _tags;
        public ObservableCollection<Tag> Tags
        {
            get { return _tags; }
            set { SetProperty(ref _tags, value); }
        }
        
        public void GetTags()
        {
            var stmt = DB.Query(@"SELECT ID, Title, Color, Notes
                                         FROM Tags");
            if(Tags == null)
                Tags = new ObservableCollection<Tag>();
            else
                Tags.Clear();
            while(stmt.Step() == SQLiteResult.ROW)
            {
                Tags.Add(new Tag((int)stmt.GetInteger(0), stmt.GetText(1), (uint)stmt.GetInteger(2), stmt.GetText(3)));
            }
        }
        public static void InsertTag(string name, Color color, string notes)
        {
            DB.QueryAndStep("INSERT INTO Tags (Title,Color,Notes) VALUES (?,?,?)", name, MyColors.ColorToUInt(color), notes);
        }
        public static void UpdateTag(int id, string name, Color color, string notes)
        {
            DB.QueryAndStep("UPDATE Tags SET Title=?, Color=?, Notes=? WHERE ID=?", name, MyColors.ColorToUInt(color),
                notes, id);
        }
        public void DeleteTag(Tag tag)
        {
            DB.QueryAndStep("BEGIN TRANSACTION");
            DB.QueryAndStep("DELETE FROM RecordsTags WHERE Tag_ID=?", tag.ID);
            DB.QueryAndStep("DELETE FROM Tags WHERE ID=?", tag.ID);
            DB.QueryAndStep("COMMIT TRANSACTION");
            Tags.Remove(tag);
        }


        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
