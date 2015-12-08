using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Penezenka_App.OtherClasses;

namespace Penezenka_App
{
    public sealed partial class LoginPage : Page
    {

        public LoginPage()
        {
            this.InitializeComponent();
        }

        private void ConfirmPasswordBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (AppSettings.TryPassword(LoginPasswordBox.Password))
            {
                App.Logged = true;
                Frame.Navigate(typeof (HubPage));
            }
            else
            {
                WrongPasswordTextBlock.Visibility = Visibility.Visible;
            }
        }
    }
}
