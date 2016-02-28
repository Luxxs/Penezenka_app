using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penezenka_App.OtherClasses
{
    class ExportToCsv
    {
        public const string SEP = ",";

        /// <summary>
        /// Converts a value to how it should output in a csv file
        /// If it has a comma, it needs surrounding with double quotes
        /// Eg Sydney, Australia -> "Sydney, Australia"
        /// Also if it contains any double quotes ("), then they need to be replaced with quad quotes[sic] ("")
        /// Eg "Dangerous Dan" McGrew -> """Dangerous Dan"" McGrew"
        /// </summary>
        public static string MakeValueCsvFriendly(object value)
        {
            if (value == null) return "";
            if (value is DateTimeOffset)
            {
                return ((DateTimeOffset)value).ToString("dd.MM.yyyy");
            }

            string output = value.ToString();
            if (output.Contains(SEP) || output.Contains("\"") || output.Contains("\n") || output.Contains("\r"))
                output = '"' + output.Replace("\"", "\"\"") + '"';

            if (output.Length > 30000) //cropping value for stupid Excel
            {
                if (output.EndsWith("\""))
                    output = output.Substring(0, 30000) + "\"";
                else
                    output = output.Substring(0, 30000);
            }

            return output.Length <= 32767 ? output : output.Substring(0, 32767);
        }
    }
}
