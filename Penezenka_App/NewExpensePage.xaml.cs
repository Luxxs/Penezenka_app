using Penezenka_App.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Penezenka_App.Model;
using SQLitePCL;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Penezenka_App
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewExpensePage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary newExpensePageViewModel = new ObservableDictionary();
        private bool editing = false;

        public NewExpensePage()
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
        public ObservableDictionary NewExpensePageViewModel
        {
            get { return this.newExpensePageViewModel; }
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
                this.newExpensePageViewModel["Record"] = (Record)e.NavigationParameter;
                NewExpenseTitle.Visibility = Visibility.Collapsed;
                this.editing = true;
            }
            else
            {
                EditExpenseTitle.Visibility = Visibility.Collapsed;
            }

            this.newExpensePageViewModel["Tags"] = new ObservableCollection<Tag>();
            ((ObservableCollection<Tag>)this.newExpensePageViewModel["Tags"]).Add(new Tag(1, "sdffds", 0xFFDC143C, "Lorem ipsum dolor amet consequetur"));
            ((ObservableCollection<Tag>)this.newExpensePageViewModel["Tags"]).Add(new Tag(1, "kkdfhgkf", 0xFF00FA9A, "d fíáqšíáčzqeíád zasdfg 89qeš7r ěč.!"));
            ((ObservableCollection<Tag>)this.newExpensePageViewModel["Tags"]).Add(new Tag(1, "ĚÍŠ ŽČĚÁ", 0xFF6495ED, "Lorem ipsum dolor amet consequetur"));
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

        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            e.Handled = true;
            Frame.Navigate(typeof(HubPage));
        }

        private void Storno_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(HubPage));
        }

        private void Ulozit_Click(object sender, RoutedEventArgs e)
        {
            if(editing)
                Record.UpdateRecord(((Record)this.newExpensePageViewModel["Record"]).ID, RecordDate.Date, RecordName.Text, 0-Convert.ToDouble(Castka.Text), RecordNotes.Text);
            else
                Record.InsertRecord(RecordDate.Date, RecordName.Text, 0-Convert.ToDouble(Castka.Text), RecordNotes.Text);
            Frame.Navigate(typeof(HubPage), true);
        }
    }
}
