using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Penezenka_App.OtherClasses;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Penezenka_App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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
            if (LoginPasswordBox.Password.Equals(AppSettings.Settings["Password"]))
            {
                App.Logged = true;
                if(fileEvent != null)
                    Frame.Navigate(typeof (HubPage), fileEvent);
                else
                    Frame.Navigate(typeof (HubPage));
            }
            else
            {
                WrongPasswordTextBlock.Visibility = Visibility.Visible;
            }
        }
    }
}
