using Penezenka_App;
using Penezenka_App.Common;
using Penezenka_App.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Penezenka_App.Model;
using Penezenka_App.OtherClasses;
using Penezenka_App.ViewModel;
using SQLitePCL;

// The Hub Application template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace Penezenka_App
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class HubPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary hubPageViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");
        private RecordsViewModel recViewModel = new RecordsViewModel();
        private TagViewModel tagViewModel = new TagViewModel();
        private Record recordToDelete;
        private Tag tagToDelete;

        public HubPage()
        {
            this.InitializeComponent();

            // Hub is only supported in Portrait orientation
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            this.NavigationCacheMode = NavigationCacheMode.Required;

            //listMonth = DateTime.Now;
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
        public ObservableDictionary HubPageViewModel
        {
            get { return this.hubPageViewModel; }
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
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            recViewModel.GetMonth(DateTime.Now.Year, DateTime.Now.Month);
            this.hubPageViewModel["Records"] = recViewModel.Records;
            this.hubPageViewModel["Records_Sum"] = ((ObservableCollection<Record>) this.hubPageViewModel["Records"]).Sum(rec => ((Record) rec).Amount);
            this.hubPageViewModel["Records_ExpenseSum"] = ((ObservableCollection<Record>) this.hubPageViewModel["Records"]).Sum(rec => (((Record) rec).Amount<0) ? ((Record) rec).Amount : 0);
            this.hubPageViewModel["Records_IncomeSum"] = ((ObservableCollection<Record>) this.hubPageViewModel["Records"]).Sum(rec => (((Record) rec).Amount>0) ? ((Record) rec).Amount : 0);
            try
            {
                this.hubPageViewModel["RecMinYear"] = new DateTimeOffset(new DateTime(RecordsViewModel.GetMinYear(), 1, 1));
                this.hubPageViewModel["RecMaxYear"] = new DateTimeOffset(new DateTime(RecordsViewModel.GetMaxYear(), 1, 1));
            }
            catch (SQLiteException)
            {
            }
            tagViewModel.GetTags();
            this.hubPageViewModel["Tags"] = tagViewModel.Tags;
            //((ObservableCollection<Tag>)this.hubPageViewModel["Tags"]).Add(new Tag(1, "sdffds", 0xFFDC143C, "Lorem ipsum dolor amet consequetur"));
            //((ObservableCollection<Tag>)this.hubPageViewModel["Tags"]).Add(new Tag(1, "kkdfhgkf", 0xFF00FA9A, "d fíáqšíáčzqeíád zasdfg 89qeš7r ěč.!"));
            //((ObservableCollection<Tag>)this.hubPageViewModel["Tags"]).Add(new Tag(1, "ĚÍŠ ŽČĚÁ", 0xFF6495ED, "Lorem ipsum dolor amet consequetur"));
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
            // TODO: Save the unique state of the page here.
        }


        /// <summary>
        /// Shows the details of an item clicked on in the <see cref="ItemPage"/>
        /// </summary>

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
        /// <param name="e">Event data that describes how this page was reached.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
        

        private void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void LayoutRoot_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (AppSettings.IsPasswordRequired() && App.Logged==false)
            {
                HubPageCommandBar.Visibility = Visibility.Collapsed;
                FlyoutBase.ShowAttachedFlyout(LayoutRoot);
            }
        }


        private void PridatVydaj(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(NewExpensePage));
        }

        /* RECORDS SECTION */
        private void RecordsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(NewExpensePage), e.ClickedItem);
        }
        private void ItemGrid_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FrameworkElement elem = sender as FrameworkElement;
            if (elem != null)
            {
                FlyoutBase.ShowAttachedFlyout(elem);
            }
        }
        private void RecordDelete_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                recordToDelete = menuFlItem.DataContext as Record;
                FlyoutBase.ShowAttachedFlyout(RecordsHubSection);
            }
        }
        private void RecordDeleteConfirmBtn_OnClick(object sender, RoutedEventArgs e)
        {
            RecordsViewModel.DeleteRecord(recordToDelete.ID, true);
            recViewModel.Records.Remove(recordToDelete);
            FlyoutBase.GetAttachedFlyout(RecordsHubSection).Hide();
        }

        private void RecordDeleteCancelBtn_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(RecordsHubSection).Hide();
        }


        /* TAGS SECTION */
        private void TagsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(NewTagPage), e.ClickedItem);
        }

        private void TagDelete_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                //Tag tag = menuFlItem.DataContext as Tag;
                tagToDelete = menuFlItem.DataContext as Tag;
                FlyoutBase.ShowAttachedFlyout(TagHubSection);
            }
        }

        private void TagDeleteConfirmBtn_OnClick(object sender, RoutedEventArgs e)
        {
            TagViewModel.DeleteTag(tagToDelete.ID);
            tagViewModel.Tags.Remove(tagToDelete);
            FlyoutBase.GetAttachedFlyout(TagHubSection).Hide();
        }

        private void TagDeleteCancelBtn_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(TagHubSection).Hide();
        }



        private void Settings_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (SettingsPage));
        }
        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (AboutPage));
        }

        private void AddTagAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (NewTagPage));
        }
    }
}
