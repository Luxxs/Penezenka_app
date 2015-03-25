using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Penezenka_App.OtherClasses
{
    class AppSettings
    {
        public static IPropertySet Settings = ApplicationData.Current.LocalSettings.Values;

        public static bool IsPasswordRequired()
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("PasswordRequired"))
            {
                return (bool)ApplicationData.Current.LocalSettings.Values["PasswordRequired"];
            }
            return false;
        }
        public static void SetPasswordRequired(bool required)
        {
            ApplicationData.Current.LocalSettings.Values["PasswordRequired"] = required;
            refreshSettings();
        }

        public static bool SetPassword(string passwd)
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("Password") && !((string) ApplicationData.Current.LocalSettings.Values["Password"]).Equals(passwd))
            {
                return false;
            }
            ApplicationData.Current.LocalSettings.Values["Password"] = passwd;
            refreshSettings();
            return true;
        }

        private static void refreshSettings()
        {
            Settings = ApplicationData.Current.LocalSettings.Values;
        }
    }
}
