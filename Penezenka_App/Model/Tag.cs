using System.Runtime.Serialization;
using Penezenka_App.OtherClasses;
using Windows.UI;

namespace Penezenka_App.Model
{
    [DataContract]
    public class Tag
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public MyColors.ColorItem Color { get; set; }
        [DataMember]
        public string Notes { get; set; }
        
        public Tag(int id, string title, uint color, string notes)
        {
            ID = id;
            Title = title;
            Color = new MyColors.ColorItem(color);
            Notes = notes;
        }
        public override bool Equals(object obj)
        {
            return (obj is Tag) && (obj as Tag).ID == ID;
        }
        public override int GetHashCode()
        {
            return ID;
        }
    }
}
