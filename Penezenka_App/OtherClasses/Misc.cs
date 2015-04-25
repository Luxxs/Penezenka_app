using System;

namespace Penezenka_App.OtherClasses
{
    static class Misc
    {
        public static DateTime IntToDateTime(int datum)
        {
            int rok = datum/10000;
            int mesic = datum/100 - rok*100;
            int den = datum - mesic*100 - rok*10000;
            return new DateTime(rok, mesic, den);
        }

        public static int DateTimeToInt(DateTime dateTime)
        {
            return dateTime.Year*10000 + dateTime.Month*100 + dateTime.Day;
        }

        public static int DateTimeToInt(DateTimeOffset dateTime)
        {
            return dateTime.Year*10000 + dateTime.Month*100 + dateTime.Day;
        }

        public static int DayOfWeekToInt(DayOfWeek dayOfWeek)
        {
            int newDayNumber = Convert.ToInt32(dayOfWeek);
            return (newDayNumber == 0) ? 7 : newDayNumber;
        }
    }
}
