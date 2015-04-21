using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Penezenka_App.Model
{
    [DataContract]
    public class Account
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
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
