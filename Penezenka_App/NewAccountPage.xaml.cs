using System;
using System.Globalization;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Penezenka_App.Common;
using Penezenka_App.Model;
using Penezenka_App.ViewModel;

namespace Penezenka_App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewAccountPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary newAccountPageViewModel = new ObservableDictionary();
        private bool editing = false;

        public NewAccountPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary NewAccountPageViewModel
        {
            get { return this.newAccountPageViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (e.NavigationParameter != null)
            {
                newAccountPageViewModel["Account"] = e.NavigationParameter as Account;
                NewAccountPageTitle.Visibility = Visibility.Collapsed;
                EditAccountPageTitle.Visibility = Visibility.Visible;
                StartBalanceStackPanel.Visibility = Visibility.Collapsed;
                editing = true;
            }
            this.newAccountPageViewModel["CurrencySymbol"] = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            SaveAppBarButton.IsEnabled = false;
            CancelAppBarButton.IsEnabled = false;
        }


        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveAppBarButton.IsEnabled = false;
            CancelAppBarButton.IsEnabled = false;
            if (editing)
            {
                AccountsViewModel.UpdateAccount(((Account) newAccountPageViewModel["Account"]).ID, TitleTextBox.Text,
                    NotesTextBox.Text);
            }
            else
            {
                double startBalance;
                try
                {
                    startBalance = (string.IsNullOrEmpty(StartBalance.Text)) ? 0 : (Convert.ToDouble(StartBalance.Text));
                    if (IsMinusCheckBox.IsChecked.Value)
                        startBalance = -startBalance;
                }
                catch (FormatException)
                {
                    WrongAmountFormatTextBlock.Visibility = Visibility.Visible;
                    SaveAppBarButton.IsEnabled = true;
                    CancelAppBarButton.IsEnabled = true;
                    return;
                }
                AccountsViewModel.InsertAccount(TitleTextBox.Text, startBalance, NotesTextBox.Text);
            }
            Frame.Navigate(typeof (AccountManagementPage));
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveAppBarButton.IsEnabled = false;
            CancelAppBarButton.IsEnabled = false;
            Frame.GoBack();
        }
    }
}
