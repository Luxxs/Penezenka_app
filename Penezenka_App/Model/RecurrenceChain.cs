using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Penezenka_App.OtherClasses;

namespace Penezenka_App.Model
{
    public class RecurrenceChain
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public int Value { get; set; }
        public bool Disabled { get; set; }
        public override string ToString()
        {
            switch (Type)
            {
                case "Y":
                    int month = Value/100;
                    int day = Value - month;
                    DateTime date = new DateTime(2000,month,day);
                    return string.Format("Každý rok, {1}. {0:MMMM}",date,day);
                case "M":
                    return string.Format("Každý měsíc, {0}. den", Value);
                case "W":
                    DateTime pomDateTime = new DateTime(2007, 1, Value);
                    int dayNumber = Misc.DayOfWeekToInt(pomDateTime.DayOfWeek);
                    if(dayNumber==1 || dayNumber==2)
                        return string.Format("Každé {0:dddd}", pomDateTime);
                    if(dayNumber==3 || dayNumber==6 || dayNumber==7)
                        return string.Format("Každá {0:dddd}", pomDateTime);
                    return string.Format("Každý {0:dddd}", pomDateTime);
                    break;
                default:
                    return Type;
            }
        }
    }
}
