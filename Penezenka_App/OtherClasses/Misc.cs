using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penezenka_App.OtherClasses
{
    static class Misc
    {
        public static int DayOfWeekToInt(DayOfWeek dayOfWeek)
        {
            int newDayNumber = Convert.ToInt32(dayOfWeek);
            return (newDayNumber == 0) ? 7 : newDayNumber;
        }
    }
}
