using System;
using System.Collections.Generic;

namespace Penezenka_App.Model
{
    public class Record
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

        public override bool Equals(object obj)
        {
            if (obj is Record)
                return ((Record) obj).ID == this.ID;
            return false;
        }

        public override int GetHashCode()
        {
            return ID;
        }
    }
}
