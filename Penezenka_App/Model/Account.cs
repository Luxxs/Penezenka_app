using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Penezenka_App.Model
{
    class Account
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }

        public override string ToString()
        {
            return Title;
        }

        public override bool Equals(object obj)
        {
            if (obj is Account)
                return ((Account) obj).ID == ID;
            return base.Equals(obj);
        }
    }
}
