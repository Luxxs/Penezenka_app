using System;

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
