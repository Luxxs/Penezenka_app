using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Penezenka_App.Database;
using Penezenka_App.Model;
using SQLitePCL;

namespace Penezenka_App.ViewModel
{
    class RecurrenceViewModel
    {
        public ObservableCollection<RecurrenceChain> RecurrenceChains { get; set; }

        public void GetRecurrenceChains()
        {
            RecurrenceChains = new ObservableCollection<RecurrenceChain>();
            var stmt = DB.Conn.Prepare("SELECT ID, Type, Value, Disabled FROM RecurrenceChains WHERE ID<>0");
            while (stmt.Step() == SQLiteResult.ROW)
            {
                RecurrenceChains.Add(new RecurrenceChain
                {
                    ID = (int)stmt.GetInteger(0),
                    Type = stmt.GetText(1),
                    Value = (int)stmt.GetInteger(2),
                    Disabled = Convert.ToBoolean(stmt.GetInteger(3))
                });
            }
        }
    }
}
