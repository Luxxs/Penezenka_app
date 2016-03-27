using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Penezenka_App.Model
{
    [DataContract]
    public class RecurrenceChain : INotifyPropertyChanged
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public int Value { get; set; }
        [DataMember]
        private bool _disabled;
        public bool Disabled
        {
            get { return _disabled; }
            set { this.SetProperty(ref this._disabled, value); }
        }

        public override string ToString()
        {
            string ret;
            switch (Type)
            {
                case "Y":
                    int month = Value/100;
                    int day = Value - month*100;
                    if (day == 29)
                    {
                        string[] monthNames =
                        {
                            "ledna", "února", "března", "dubna", "května", "června", "července",
                            "srpna", "září", "října", "listopadu", "prosince"
                        };
                        ret = "Každý rok, posledního " + monthNames[month - 1];
                    }
                    else
                    {
                        DateTime date = new DateTime(2000, month, day);
                        ret = $"Každý rok, {date:M}";
                    }
                    break;
                case "M":
                    ret = "Každý měsíc, "+(Value == 29 ? "poslední den v měsíci" : Value+ ". den");
                    break;
                case "W":
                    DateTime pomDateTime = new DateTime(2007, 1, Value);
                    if(Value==1 || Value==2)
                        ret = $"Každé {pomDateTime:dddd}";
                    else if(Value==3 || Value==6 || Value==7)
                        ret = $"Každá {pomDateTime:dddd}";
                    else
                        ret = $"Každý {pomDateTime:dddd}";
                    break;
                default:
                    ret =  Type;
                    break;
            }
            return ret + ((ID!=0 && Disabled) ? " (zrušeno)" : "");
        }

        #region INotifyPropertyChanged members
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;
            storage = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
