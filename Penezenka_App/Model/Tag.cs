using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Penezenka_App.Database;
using Penezenka_App.OtherClasses;
using SQLitePCL;

namespace Penezenka_App.Model
{
    public class Tag
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public MyColors.ColorItem Color { get; set; }
        public string Notes { get; set; }
        public int NumRecords { get; set; }
        
        public Tag(int id, string title, uint color, string notes, int numRec=0)
        {
            ID = id;
            Title = title;
            Color = new MyColors.ColorItem(color);
            Notes = notes;
            NumRecords = numRec;
        }
    }
}
