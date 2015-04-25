using Penezenka_App.OtherClasses;

namespace Penezenka_App.Model
{
    public class Tag
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public MyColors.ColorItem Color { get; set; }
        public string Notes { get; set; }
        
        public Tag(int id, string title, uint color, string notes)
        {
            ID = id;
            Title = title;
            Color = new MyColors.ColorItem(color);
            Notes = notes;
        }
    }
}
