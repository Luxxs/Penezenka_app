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
        private readonly ObservableDictionary upravenejViewModel = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");
        private DateTime listMonth;

        public HubPage()
        {
            this.InitializeComponent();

            // Hub is only supported in Portrait orientation
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            this.NavigationCacheMode = NavigationCacheMode.Required;

            listMonth = DateTime.Now;
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
        public ObservableDictionary UctyViewModel
        {
            get { return this.upravenejViewModel; }
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
            this.upravenejViewModel["Polozky"] = null;
            this.upravenejViewModel["Polozky"] = Polozka.GetMonth(DateTime.Now.Year, DateTime.Now.Month);
            this.upravenejViewModel["Polozky_Suma"] = ((ObservableCollection<Polozka>) this.upravenejViewModel["Polozky"]).Sum(pol => ((Polozka) pol).Castka);
            this.upravenejViewModel["Polozky_VydajeSuma"] = ((ObservableCollection<Polozka>) this.upravenejViewModel["Polozky"]).Sum(pol => (((Polozka) pol).Castka<0) ? ((Polozka) pol).Castka : 0);
            this.upravenejViewModel["Polozky_PrijmySuma"] = ((ObservableCollection<Polozka>) this.upravenejViewModel["Polozky"]).Sum(pol => (((Polozka) pol).Castka>0) ? ((Polozka) pol).Castka : 0);
            try
            {
                this.upravenejViewModel["PolMinYear"] = new DateTimeOffset(new DateTime(Polozka.GetMinYear(), 1, 1));
                this.upravenejViewModel["PolMaxYear"] = new DateTimeOffset(new DateTime(Polozka.GetMaxYear(), 1, 1));
            }
            catch (SQLiteException)
            {
            }
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
        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(ZadaniVydajePage), e.ClickedItem);
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

        private void PridatVydaj(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ZadaniVydajePage));
        }

        private void Grid_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FrameworkElement elem = sender as FrameworkElement;
            if (elem != null)
            {
                FlyoutBase.ShowAttachedFlyout(elem);
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                Polozka polozka = menuFlItem.DataContext as Polozka;
                Polozka.SmazPolozku(polozka.ID);
                this.upravenejViewModel["Polozky"] = Polozka.GetMonth(DateTime.Now.Year, DateTime.Now.Month);
            }
        }

        private void MonthPlus_BtnCLick(object sender, RoutedEventArgs e)
        {
            listMonth.AddMonths(1);
        }
    }
}
