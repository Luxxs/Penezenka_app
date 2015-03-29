using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace Penezenka_App.Model
{
    class RecurrenceChain
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public int Value { get; set; }
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
                    return string.Format("Každý {0:dddd}", new DateTime(2007, 1, Value));
                default:
                    return Type;
            }
        }
    }
}
