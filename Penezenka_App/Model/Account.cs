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
        public double Balance { get; set; }
        public string Notes { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }
}
