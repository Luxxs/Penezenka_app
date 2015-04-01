using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Penezenka_App.OtherClasses;

namespace Penezenka_App.SampleData
{
    class ColorsModel
    {
        public ObservableCollection<MyColors.ColorItem> ColorItems
        {
            get
            {
                ObservableCollection<MyColors.ColorItem> colors = new ObservableCollection<MyColors.ColorItem>();
                for (int i = 0; i < MyColors.UIntColors.Length; i++)
                {
                    colors.Add(new MyColors.ColorItem(MyColors.UIntColors[i], MyColors.ColorNames[i]));
                }
                return colors;
            }
        }
    }
}
