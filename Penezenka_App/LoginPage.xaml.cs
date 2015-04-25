using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Penezenka_App.OtherClasses;

namespace Penezenka_App
{
    public sealed partial class LoginPage : Page
    {
        private FileActivatedEventArgs fileEvent;

        public LoginPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is FileActivatedEventArgs)
                fileEvent = (FileActivatedEventArgs)e.Parameter;
        }

        private void ConfirmPasswordBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (AppSettings.TryPassword(LoginPasswordBox.Password))
            {
                App.Logged = true;
                Frame.Navigate(typeof (HubPage), fileEvent);
            }
            else
            {
                WrongPasswordTextBlock.Visibility = Visibility.Visible;
            }
        }
    }
}
