using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Penezenka_App.Database;
using SQLitePCL;

namespace Penezenka_App.Model
{
    class Record
    {
        public int ID { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Title { get; set; }
        public double Amount { get; set; }
        public string Notes { get; set; }
        public Account Account { get; set; }
        public List<Tag> Tags { get; set; }
        public RecurrenceChain RecurrenceChain { get; set; }
        public bool Automatically { get; set; }
    }
}
