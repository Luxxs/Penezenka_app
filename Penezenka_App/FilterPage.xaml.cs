using Penezenka_App.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Penezenka_App.Model;
using Penezenka_App.ViewModel;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Penezenka_App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
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
                filter = e.NavigationParameter as RecordsViewModel.Filter;
                filterPageViewModel["DateFrom"] = filter.StartDateTime;
                filterPageViewModel["DateTo"] = filter.EndDateTime;
                filterPageViewModel["IsAllTags"] = filter.AllTags;
                filterPageViewModel["ActualTags"] = filter.Tags;
                filterPageViewModel["IsAllAccounts"] = filter.AllAccounts;
                filterPageViewModel["ActualAccounts"] = filter.Accounts;
                /*if (filter.Accounts != null && NewAccountsListView!=null) {
                    foreach (var account in filter.Accounts)
                    {
                        NewAccountsListView.SelectedItems.Add(account);
                    }
                }*/
            }
            filterPageViewModel["MinDate"] = (DateTimeOffset) RecordsViewModel.GetMinDate();
            filterPageViewModel["MaxDate"] = (DateTimeOffset) RecordsViewModel.GetMaxDate();
            tagViewModel.GetTags();
            filterPageViewModel["Tags"] = tagViewModel.Tags;
            accountsViewModel.GetAccounts(true);
            filterPageViewModel["Accounts"] = accountsViewModel.Accounts;
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
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
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void AcceptFilter_Click(object sender, RoutedEventArgs e)
        {
            if (!AllAccountsCheckBox.IsChecked.Value && NewAccountsListView.SelectedItems.Count == 0)
            {
                EmptyNewAccountsListView.Visibility = Visibility.Visible;
                return;
            }

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
                // cannot convert List<object> to List<Tag> :/
                // nejde foreach (var selectedItem in NewTagsGridView.SelectedItems)
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
            Frame.Navigate(typeof (HubPage), newFilter);
        }

        private void CancelFilter_Click(object sender, RoutedEventArgs e)
        {
            //Frame.Navigate(typeof (HubPage));
            Frame.GoBack();
        }

        private void NewAccountsListView_Loaded(object sender, RoutedEventArgs e)
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
