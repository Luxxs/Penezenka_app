﻿using System.Runtime.Serialization;

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
            return (ID == 0) ? "<žádný>" : Title;
        }

        public override bool Equals(object obj)
        {
            if (obj is Account)
                return ((Account) obj).ID == ID;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return ID;
        }
    }
}
