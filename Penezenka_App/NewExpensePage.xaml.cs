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
using Penezenka_App.ViewModel;
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
        private TagViewModel tagViewModel = new TagViewModel();
        private List<Tag> selectedTags = new List<Tag>(); 
        private bool editing = false;
        private class DayOfWeekMap
        {
            public int Day;
            public new string ToString()
            {
                return (new DateTime(2007,1,Day)).ToString("dddd");
            }
        }
        private class MonthNameMap
        {
            public int Month;
            public new string ToString()
            {
                return (new DateTime(2000,Month,1)).ToString("MMMM");
            }
        }

        private class DayInMonthMap
        {
            public int Day;
            public new string ToString()
            {
                return (Day == 29) ? "Poslední den v měsíci" : Day.ToString();
            }
        }

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
                NewExpenseTitle.Visibility = Visibility.Collapsed;
                Record record = (Record)e.NavigationParameter;
                this.newExpensePageViewModel["Record"] = record;
                foreach (var tag in record.Tags)
                {
                    TagsGridView.SelectedItems.Add(tag);
                }
                //změnit comboBoxy na fixní výdaje
                this.editing = true;
            }
            else
            {
                EditExpenseTitle.Visibility = Visibility.Collapsed;
            }

            /*var dayMonth = new ObservableCollection<string>();
            for (int i = 1; i <= 28; i++)
            {
                dayMonth.Add(i.ToString());
            }
            dayMonth.Add("Poslední den v měsíci");

            this.newExpensePageViewModel["RecurringDayInMonth"] = dayMonth;*/
            //todo: ToString() problem
            this.newExpensePageViewModel["RecurringDayInMonth"] = new DayInMonthMap[29];
            for (int i = 0; i < 29; i++)
            {
                ((DayInMonthMap[])newExpensePageViewModel["RecurringDayInMonth"])[i] = new DayInMonthMap(){Day=i+1};
            }
            this.newExpensePageViewModel["RecurringDayOfWeek"] = new DayOfWeekMap[7];
            for (int i = 0; i < 7; i++)
            {
                ((DayOfWeekMap[])newExpensePageViewModel["RecurringDayOfWeek"])[i] = new DayOfWeekMap(){Day=i+1};

            }
            this.newExpensePageViewModel["RecurringMonth"] = new MonthNameMap[12];
            for (int i = 0; i < 12; i++)
            {
                ((MonthNameMap[]) newExpensePageViewModel["RecurringMonth"])[i] = new MonthNameMap(){Month=i+1};
            }

            /*DateTimeOffset yearDate = new DateTimeOffset();
            this.newExpensePageViewModel["RecurringYearDate"] = yearDate;*///.AddYears(-yearDate.Year);
            tagViewModel.GetTags();
            this.newExpensePageViewModel["Tags"] = tagViewModel.Tags;
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
            string title = (string.IsNullOrEmpty(RecordTitle.Text)) ? "<Položka bez názvu>" : RecordTitle.Text;
            double amount = 0 - Convert.ToDouble(RecordAmount.Text);

            List<Tag> tags = new List<Tag>();
            for (int i = 0; i < TagsGridView.SelectedItems.Count; i++)
                tags.Add((Tag)TagsGridView.SelectedItems[i]);

            string recurrenceType = null;
            int recurrenceValue = 0;
            if (RecordRecurring.IsChecked.Value)
            {
                switch (RecPatternComboBox.SelectedIndex)
                {
                    case 0:
                        if (RecMonthComboBox.SelectedValue == null || RecDayInMonthComboBox.SelectedValue == null)
                        {
                            EmptyRecurreneceValueTextBlock.Visibility = Visibility.Visible;
                            return;
                        }
                        recurrenceType = "Y";
                        recurrenceValue = Convert.ToInt32(((MonthNameMap)RecMonthComboBox.SelectedValue).Month)*100 + Convert.ToInt32(RecDayInMonthComboBox.SelectedValue);
                        break;
                    case 1:
                        if (RecDayInMonthComboBox.SelectedValue == null)
                        {
                            EmptyRecurreneceValueTextBlock.Visibility = Visibility.Visible;
                            return;
                        }
                        recurrenceType = "M";
                        recurrenceValue = Convert.ToInt32(((DayInMonthMap)RecDayInMonthComboBox.SelectedValue).Day);
                        break;
                    case 2:
                        if (RecDayOfWeekComboBox.SelectedValue == null)
                        {
                            EmptyRecurreneceValueTextBlock.Visibility = Visibility.Visible;
                            return;
                        }
                        recurrenceType = "W";
                        recurrenceValue = Convert.ToInt32(((DayOfWeekMap)RecDayOfWeekComboBox.SelectedValue).Day);
                        break;
                }
            }
            if(editing)
                RecordsViewModel.UpdateRecord(((Record)this.newExpensePageViewModel["Record"]).ID, RecordDate.Date, title, amount, RecordNotes.Text, tags, recurrenceType, recurrenceValue);
            else
                RecordsViewModel.InsertRecord(RecordDate.Date, title, amount, RecordNotes.Text, tags, recurrenceType, recurrenceValue);

            Frame.Navigate(typeof(HubPage), true);
        }

        private void TagsGridView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // SelectedItems nemá setter :(
            if (editing)
            {
                foreach (var tag in ((Record)newExpensePageViewModel["Record"]).Tags)
                {
                    TagsGridView.SelectedItems.Add(tag);
                }
            }
        }

        private void RecPatternComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecPatternComboBox != null)
            {
                switch (RecPatternComboBox.SelectedIndex)
                {
                    case 0:
                        RecDayInMonthComboBox.Visibility = Visibility.Visible;
                        RecDayOfWeekComboBox.Visibility = Visibility.Collapsed;
                        RecMonthComboBox.Visibility = Visibility.Visible;
                        break;
                    case 1:
                        RecDayInMonthComboBox.Visibility = Visibility.Visible;
                        RecDayOfWeekComboBox.Visibility = Visibility.Collapsed;
                        RecMonthComboBox.Visibility = Visibility.Collapsed;
                        break;
                    case 2:
                        RecDayInMonthComboBox.Visibility = Visibility.Collapsed;
                        RecDayOfWeekComboBox.Visibility = Visibility.Visible;
                        RecMonthComboBox.Visibility = Visibility.Collapsed;
                        break;
                }
            }
        }
    }
}
