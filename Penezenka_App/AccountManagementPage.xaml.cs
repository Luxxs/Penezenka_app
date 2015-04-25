using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Penezenka_App.Common;
using Penezenka_App.Model;
using Penezenka_App.ViewModel;

namespace Penezenka_App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AccountManagementPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary accountsPageViewModel = new ObservableDictionary();
        private AccountsViewModel accountsViewModel = new AccountsViewModel();
        private AccountsViewModel otherAccoutsViewModel = new AccountsViewModel();
        private Account accountToDelete;

        public AccountManagementPage()
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
        public ObservableDictionary AccountsPageViewModel
        {
            get { return this.accountsPageViewModel; }
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
            accountsViewModel.GetAccounts();
            this.accountsPageViewModel["Accounts"] = accountsViewModel.Accounts;
            otherAccoutsViewModel.GetAccounts(true);
            this.accountsPageViewModel["OtherAccounts"] = otherAccoutsViewModel.Accounts;
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
            AddAccountAppBarButton.IsEnabled = false;
            e.Handled = true;
            int backStackCount = Frame.BackStack.Count;
            for (int i = 1; i < backStackCount; i++)
            {
                Frame.BackStack.RemoveAt(1);
            }
            Frame.GoBack();
        }


        private void AccountsListView_OnItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof (NewAccountPage), e.ClickedItem);
        }

        private void AddAccountAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            AddAccountAppBarButton.IsEnabled = false;
            Frame.Navigate(typeof (NewAccountPage));
        }


        
        private void Item_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FrameworkElement elem = sender as FrameworkElement;
            if (elem != null)
            {
                FlyoutBase.ShowAttachedFlyout(elem);
            }
        }
        private void EditAccount_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                Frame.Navigate(typeof(NewAccountPage), menuFlItem.DataContext as Account);
            }
        }
        private void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                accountToDelete = menuFlItem.DataContext as Account;
                if (AccountsViewModel.GetNumOfRecordsInAccount(accountToDelete.ID) > 0)
                    FlyoutBase.ShowAttachedFlyout(LayoutRoot);
                else
                    accountsViewModel.DeleteAccount(accountToDelete.ID);
            }
        }
        private void FlyoutBase_OnOpening(object sender, object e)
        {
            var otherAccounts = new ObservableCollection<Account>(otherAccoutsViewModel.Accounts);
            otherAccounts.Remove(accountsViewModel.Accounts.First(x => x.ID == accountToDelete.ID));
            TransferToAccountComboBox.ItemsSource = otherAccounts;
            TransferToAccountComboBox.SelectedIndex = 0;
        }

        private void PickerFlyout_OnConfirmed(PickerFlyout sender, PickerConfirmedEventArgs args)
        {
            if(DeleteRecordsCheckBox.IsChecked.Value)
                accountsViewModel.DeleteAccount(accountToDelete.ID);
            else
                accountsViewModel.DeleteAccount(accountToDelete.ID, (TransferToAccountComboBox.SelectedItem as Account).ID);
        }
    }
}
