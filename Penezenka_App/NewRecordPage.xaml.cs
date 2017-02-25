using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;
using Penezenka_App.Common;
using Penezenka_App.Converters;
using Penezenka_App.Database;
using Penezenka_App.Model;
using Penezenka_App.ViewModel;
using Penezenka_App.OtherClasses;

namespace Penezenka_App
{
    public sealed partial class NewRecordPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary newExpensePageViewModel = new ObservableDictionary();
        private AccountsViewModel accountsViewModel = new AccountsViewModel();
        private TagViewModel tagViewModel = new TagViewModel();
        private RecordsViewModel.Filter filter;
        private Record record;
        private bool editing = false;
        private bool income = false;
        private class DayOfWeekPair
        {
            public int Day;
            public override string ToString()
            {
                return (new DateTime(2007,1,Day)).ToString("dddd");
            }
        }
        private class MonthNamePair
        {
            public int Month;
            public override string ToString()
            {
                return (new DateTime(2000,Month,1)).ToString("MMMM");
            }
        }
        private class DayInMonthPair
        {
            public int Day;
            public override string ToString()
            {
                return (Day == 29) ? "Poslední den v měsíci" : Day+".";
            }
        }

        public NewRecordPage()
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
        public ObservableDictionary NewExpensePageViewModel
        {
            get { return newExpensePageViewModel; }
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
            var navigationParam = Export.DeserializeObjectFromJsonString<IdFilterPair>((string)e.NavigationParameter);
            filter = navigationParam.Filter;
            if (navigationParam.Id == 0)
            {
                income = true;
                NewIncomeTitle.Visibility = Visibility.Visible;
            }
            else if (navigationParam.Id < 0)
            {
                NewExpenseTitle.Visibility = Visibility.Visible;
                MinusSign.Visibility = Visibility.Visible;
            }
            else
            {
                record = RecordsViewModel.GetRecordByID(navigationParam.Id);
                newExpensePageViewModel["Record"] = record;
                if (record.Amount < 0)
                {
                    EditExpenseTitle.Visibility = Visibility.Visible;
                    MinusSign.Visibility = Visibility.Visible;
                    MinusSign.SetBinding(VisibilityProperty, new Binding() {Path = new PropertyPath("IsChecked"), ElementName = "ChangeToIncomeCheckBox", Converter = new CountToVisibilityConverter(), ConverterParameter = true});
                    ChangeToIncomeCheckBox.Visibility = Visibility.Visible;
                }
                else
                {
                    income = true;
                    EditIncomeTitle.Visibility = Visibility.Visible;
                    ChangeToExpenseCheckBox.Visibility = Visibility.Visible;
                }
                record.Amount = Math.Abs(record.Amount);

                editing = true;
            }

            newExpensePageViewModel["CurrencySymbol"] = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;

            newExpensePageViewModel["RecurringDayInMonth"] = new DayInMonthPair[29];
            for (int i = 0; i < 29; i++)
            {
                ((DayInMonthPair[])newExpensePageViewModel["RecurringDayInMonth"])[i] = new DayInMonthPair(){Day=i+1};
            }
            newExpensePageViewModel["RecurringDayOfWeek"] = new DayOfWeekPair[7];
            for (int i = 0; i < 7; i++)
            {
                ((DayOfWeekPair[])newExpensePageViewModel["RecurringDayOfWeek"])[i] = new DayOfWeekPair(){Day=i+1};

            }
            newExpensePageViewModel["RecurringMonth"] = new MonthNamePair[12];
            for (int i = 0; i < 12; i++)
            {
                ((MonthNamePair[]) newExpensePageViewModel["RecurringMonth"])[i] = new MonthNamePair(){Month=i+1};
            }
            
            accountsViewModel.GetAccounts(record != null);
            newExpensePageViewModel["Accounts"] = accountsViewModel.Accounts;
            tagViewModel.GetTags();
            newExpensePageViewModel["Tags"] = tagViewModel.Tags;
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

        private void SaveExpense_OnClick(object sender, RoutedEventArgs e)
        {
            double amount;
            if (ChangeToExpenseCheckBox.IsChecked.Value || ChangeToIncomeCheckBox.IsChecked.Value)
                income = !income;
            try
            {
                if(income)
                    amount = Convert.ToDouble(RecordAmount.Text);
                else
                    amount = -Convert.ToDouble(RecordAmount.Text);
                WrongAmountFormatTextBlock.Visibility = Visibility.Collapsed;
            }
            catch (FormatException)
            {
                WrongAmountFormatTextBlock.Visibility = Visibility.Visible;
                return;
            }

            List<Tag> tags = NewTagsGridView.SelectedItems.Cast<Tag>().ToList();

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
                        recurrenceValue = Convert.ToInt32(((MonthNamePair)RecMonthComboBox.SelectedValue).Month)*100 + Convert.ToInt32(((DayInMonthPair)RecDayInMonthComboBox.SelectedValue).Day);
                        break;
                    case 1:
                        if (RecDayInMonthComboBox.SelectedValue == null)
                        {
                            EmptyRecurreneceValueTextBlock.Visibility = Visibility.Visible;
                            return;
                        }
                        recurrenceType = "M";
                        recurrenceValue = Convert.ToInt32(((DayInMonthPair)RecDayInMonthComboBox.SelectedValue).Day);
                        break;
                    case 2:
                        if (RecDayOfWeekComboBox.SelectedValue == null)
                        {
                            EmptyRecurreneceValueTextBlock.Visibility = Visibility.Visible;
                            return;
                        }
                        recurrenceType = "W";
                        recurrenceValue = Convert.ToInt32(((DayOfWeekPair)RecDayOfWeekComboBox.SelectedValue).Day);
                        break;
                }
            }
            SaveAppBarButton.IsEnabled = false;
            CancelAppBarButton.IsEnabled = false;

            int accountId = (RecordAccountComboBox.SelectedItem == null) ? 0 : ((Account) RecordAccountComboBox.SelectedItem).ID;
            if (editing)
            {
                Record record = (Record) newExpensePageViewModel["Record"];
                RecordsViewModel.UpdateRecord(record.ID, accountId, RecordDate.Date, RecordTitle.Text, amount, RecordNotes.Text,
                    tags, record.RecurrenceChain.ID, recurrenceType, recurrenceValue);
            }
            else
            {
                RecordsViewModel.InsertRecord(accountId, RecordDate.Date, RecordTitle.Text, amount, RecordNotes.Text, tags, recurrenceType, recurrenceValue);
            }

            if(recurrenceType!=null)
                DB.AddRecurrentRecords();

            Frame.Navigate(typeof(HubPage), Export.SerializeObjectToJsonString<RecordsViewModel.Filter>(filter));
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            SaveAppBarButton.IsEnabled = false;
            CancelAppBarButton.IsEnabled = false;
            Frame.GoBack();
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

        private void RecurrencyStackPanel_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (editing && newExpensePageViewModel.ContainsKey("Record"))
            {
                RecurrenceChain recurrency = ((Record) newExpensePageViewModel["Record"]).RecurrenceChain;
                if (recurrency.ID != 0)
                {
                    switch (recurrency.Type)
                    {
                        case "Y":
                            RecPatternComboBox.SelectedIndex = 0;
                            RecPatternComboBox_OnSelectionChanged(null, null);
                            int month = recurrency.Value/100;
                            int day = recurrency.Value - month*100;
                            RecDayInMonthComboBox.SelectedIndex = day - 1;
                            RecMonthComboBox.SelectedIndex = month - 1;
                            break;
                        case "M":
                            RecPatternComboBox.SelectedIndex = 1;
                            RecPatternComboBox_OnSelectionChanged(null, null);
                            RecDayInMonthComboBox.SelectedIndex = recurrency.Value - 1;
                            break;
                        case "W":
                            RecPatternComboBox.SelectedIndex = 2;
                            RecPatternComboBox_OnSelectionChanged(null, null);
                            RecDayOfWeekComboBox.SelectedIndex = recurrency.Value - 1;
                            break;
                    }
                }
            }
        }

        private void NewTagsGridView_Loaded(object sender, RoutedEventArgs e)
        {
            if (record != null && record.Tags != null)
            {
                foreach (Tag tag in record.Tags)
                {
                    NewTagsGridView.SelectedItems.Add(tag);
                }
            }
        }
    }
}
