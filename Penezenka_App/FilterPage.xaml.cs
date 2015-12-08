using System.Collections.Generic;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Penezenka_App.Common;
using Penezenka_App.Model;
using Penezenka_App.OtherClasses;
using Penezenka_App.ViewModel;

namespace Penezenka_App
{
    public sealed partial class FilterPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary filterPageViewModel = new ObservableDictionary();
        private RecordsViewModel.Filter filter = new RecordsViewModel.Filter();
        private TagViewModel tagViewModel = new TagViewModel();
        private AccountsViewModel accountsViewModel = new AccountsViewModel();

        public FilterPage()
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
        public ObservableDictionary FilterPageViewModel
        {
            get { return this.filterPageViewModel; }
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
                filter = (RecordsViewModel.Filter) Export.DeserializeObjectFromJsonString(e.NavigationParameter as string, typeof(RecordsViewModel.Filter));
                filterPageViewModel["DateFrom"] = filter.StartDateTime;
                filterPageViewModel["DateTo"] = filter.EndDateTime;
                filterPageViewModel["IsAllTags"] = filter.AllTags;
                filterPageViewModel["ActualTags"] = filter.Tags;
                filterPageViewModel["IsAllAccounts"] = filter.AllAccounts;
                filterPageViewModel["ActualAccounts"] = filter.Accounts;
            }
            filterPageViewModel["MinDate"] = RecordsViewModel.GetMinDate();
            filterPageViewModel["MaxDate"] = RecordsViewModel.GetMaxDate();
            tagViewModel.GetTags();
            filterPageViewModel["Tags"] = tagViewModel.Tags;
            accountsViewModel.GetAccounts(true);
            filterPageViewModel["Accounts"] = accountsViewModel.Accounts;
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
            AcceptFilterAppBarButton.IsEnabled = false;
            CancelAppBarButton.IsEnabled = false;
        }

        private void AcceptFilter_OnClick(object sender, RoutedEventArgs e)
        {
            if (!AllAccountsCheckBox.IsChecked.Value && NewAccountsListView.SelectedItems.Count == 0)
            {
                EmptyNewAccountsListView.Visibility = Visibility.Visible;
                return;
            }
            AcceptFilterAppBarButton.IsEnabled = false;
            CancelAppBarButton.IsEnabled = false;

            var newFilter = new RecordsViewModel.Filter
            {
                StartDateTime = DateFromDatePicker.Date,
                EndDateTime = DateToDatePicker.Date,
                AllTags = AllTagsCheckBox.IsChecked.Value,
                AllAccounts = AllAccountsCheckBox.IsChecked.Value
            };
            if (!AllTagsCheckBox.IsChecked.Value)
            {
                newFilter.Tags = new List<Tag>();
                for (int i = 0; i < NewTagsGridView.SelectedItems.Count; i++)
                {
                    newFilter.Tags.Add((Tag) NewTagsGridView.SelectedItems[i]);
                }
            }
            if (!AllAccountsCheckBox.IsChecked.Value)
            {
                newFilter.Accounts = new List<Account>();
                for (int i = 0; i < NewAccountsListView.SelectedItems.Count; i++)
                {
                    newFilter.Accounts.Add((Account) NewAccountsListView.SelectedItems[i]);
                }
            }
            string newFilterString = Export.SerializeObjectToJsonString(newFilter, typeof (RecordsViewModel.Filter));
            App.Imported = false;
            Frame.Navigate(typeof (HubPage), newFilterString);
        }

        private void CancelFilter_OnClick(object sender, RoutedEventArgs e)
        {
            AcceptFilterAppBarButton.IsEnabled = false;
            CancelAppBarButton.IsEnabled = false;
            Frame.GoBack();
        }

        private void NewAccountsListView_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (filterPageViewModel.ContainsKey("ActualAccounts") && filterPageViewModel["ActualAccounts"] != null)
            {
                var listView = sender as ListView;
                foreach (var account in filterPageViewModel["ActualAccounts"] as List<Account>)
                {
                    listView.SelectedItems.Add(account);
                }
            }
        }
    }
}
