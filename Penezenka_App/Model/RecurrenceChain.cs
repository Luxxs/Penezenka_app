using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Penezenka_App.OtherClasses;

namespace Penezenka_App.Model
{
    public class RecurrenceChain : INotifyPropertyChanged
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public int Value { get; set; }
        private bool _disabled;
        // Stejně neaktualizuje položku ve výpise.
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
                    DateTime date = new DateTime(2000,month,day);
                    ret = string.Format("Každý rok, {0:M}", date);
                    break;
                case "M":
                    ret = string.Format("Každý měsíc, {0}. den", Value);
                    break;
                case "W":
                    DateTime pomDateTime = new DateTime(2007, 1, Value);
                    if(Value==1 || Value==2)
                        ret = string.Format("Každé {0:dddd}", pomDateTime);
                    else if(Value==3 || Value==6 || Value==7)
                        ret = string.Format("Každá {0:dddd}", pomDateTime);
                    else
                        ret = string.Format("Každý {0:dddd}", pomDateTime);
                    break;
                default:
                    ret =  Type;
                    break;
            }
            return ret + ((ID!=0 && Disabled) ? " (zrušeno)" : "");
        }

        
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value))
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
    }
}
