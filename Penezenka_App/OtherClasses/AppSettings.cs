using Windows.Foundation.Collections;
using Windows.Storage;

namespace Penezenka_App.OtherClasses
{
    static class AppSettings
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
            if (required == false)
                ApplicationData.Current.LocalSettings.Values["Password"] = string.Empty;
            RefreshSettings();
        }

        public static void SetPassword(string passwd)
        {
            ApplicationData.Current.LocalSettings.Values["Password"] = passwd;
            RefreshSettings();
        }

        public static bool TryPassword(string passwd)
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("Password") && ((string) ApplicationData.Current.LocalSettings.Values["Password"]).Equals(passwd))
            {
                return true;
            }
            return false;
        }

        private static void RefreshSettings()
        {
            Settings = ApplicationData.Current.LocalSettings.Values;
        }
    }
}
