using Penezenka_App;
using Penezenka_App.Common;
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
using Windows.UI;
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
using Windows.UI.Xaml.Shapes;
using Penezenka_App.Database;
using Penezenka_App.Model;
using Penezenka_App.OtherClasses;
using Penezenka_App.ViewModel;
using SQLitePCL;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

// The Hub Application template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace Penezenka_App
{
    /// <summary>
    /// A page that displays a grouped collection of items.
    /// </summary>
    public sealed partial class HubPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary hubPageViewModels = new ObservableDictionary();
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");
        private RecordsViewModel recordsViewModel = new RecordsViewModel();
        private RecordsViewModel pendingRecordsViewModel = new RecordsViewModel();
        private AccountsViewModel accViewModel = new AccountsViewModel();
        private TagViewModel tagViewModel = new TagViewModel();

        private RecordsViewModel.Filter filter = new RecordsViewModel.Filter
        {
            StartDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            EndDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1),
            AllTags = true,
            AllAccounts = true
        };
        private Record recordToDelete;
        private Record recordToTransfer;
        private Chart pieChartExpenses;
        private Chart pieChartIncome;
        private Chart lineChart;
        private Tag tagToDelete;

        public HubPage()
        {
            this.InitializeComponent();

            // Hub is only supported in Portrait orientation
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            this.NavigationCacheMode = NavigationCacheMode.Required;

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
        public ObservableDictionary HubPageViewModels
        {
            get { return this.hubPageViewModels; }
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
            if(e.NavigationParameter != null && e.NavigationParameter is RecordsViewModel.Filter)
                filter = e.NavigationParameter as RecordsViewModel.Filter;
            if (filter.StartDateTime == new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) &&
                filter.EndDateTime == new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1))
                RecordsHubSection.Header = "TENTO MĚSÍC";
            else
                RecordsHubSection.Header = "VYBRANÉ ZÁZNAMY";
            DB.AddRecurrentRecords();
            hubPageViewModels["RecordsViewModel"] = recordsViewModel;
            (hubPageViewModels["RecordsViewModel"] as RecordsViewModel).GetFilteredRecords(filter);
            if (pieChartExpenses != null)
            {
                if (recordsViewModel.ExpensesPerTagChartMap.Count == 0)
                    pieChartExpenses.Visibility = Visibility.Collapsed;
                else
                {
                    ((DataPointSeries) pieChartExpenses.Series[0]).ItemsSource = (hubPageViewModels["RecordsViewModel"] as RecordsViewModel).ExpensesPerTagChartMap;
                    pieChartExpenses.Visibility = Visibility.Visible;
                }
            }
            if (pieChartIncome != null)
            {
                if (recordsViewModel.IncomePerTagChartMap.Count == 0)
                    pieChartIncome.Visibility = Visibility.Collapsed;
                else
                {
                    ((DataPointSeries) pieChartIncome.Series[0]).ItemsSource = (hubPageViewModels["RecordsViewModel"] as RecordsViewModel).IncomePerTagChartMap;
                    pieChartIncome.Visibility = Visibility.Visible;
                }
            }
            if(lineChart!=null)
                ((DataPointSeries) lineChart.Series[0]).ItemsSource = (hubPageViewModels["RecordsViewModel"] as RecordsViewModel).BalanceInTime;

            pendingRecordsViewModel.GetRecurrentRecords(true);
            hubPageViewModels["PendingRecordsViewModel"] = pendingRecordsViewModel;

            accViewModel.GetAccounts(true);
            hubPageViewModels["Accounts"] = accViewModel.Accounts;

            tagViewModel.GetTags();
            this.hubPageViewModels["Tags"] = tagViewModel.Tags;
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
            e.Handled = true;
            //neanimuje se :/
            /*if(!Hub.SectionsInView[0].Equals(Hub.Sections[0]))
                Hub.ScrollToSection(Hub.Sections[0]);
            else*/
                Application.Current.Exit();
        }
        
        /* HUB CHANGES & LOADING */
        private void Hub_OnSectionsInViewChanged(object sender, SectionsInViewChangedEventArgs e)
        {
            string first, second, third, removed, added;
            if (Hub.SectionsInView.Count > 0)
                first = Hub.SectionsInView[0].Name;
            if (Hub.SectionsInView.Count > 1)
                second = Hub.SectionsInView[1].Name;
            if (Hub.SectionsInView.Count > 2)
                third = Hub.SectionsInView[2].Name;
            if (e.AddedSections.Count > 0)
                added = e.AddedSections[0].Name;
            if (e.RemovedSections.Count > 0)
                removed = e.RemovedSections[0].Name;

            if(e.RemovedSections.Count>0 && Hub.SectionsInView[0].Name.Equals("TagHubSection"))
            {
                AddTagAppBarButton.Visibility = Visibility.Visible;
            }
            else
            {
                AddTagAppBarButton.Visibility = Visibility.Collapsed;
            }
            if(e.RemovedSections.Count>0 && Hub.SectionsInView[0].Name.Equals("RecordsHubSection") ||
                e.RemovedSections.Count>0 && Hub.SectionsInView[0].Name.Equals("ChartsHubSection"))
            {
                FilterAppBarButton.Visibility = Visibility.Visible;
            }
            else
            {
                FilterAppBarButton.Visibility = Visibility.Collapsed;
            }

        }
        private void PieChartExpenses_Loaded(object sender, RoutedEventArgs e)
        {
            pieChartExpenses = (Chart) sender;
            refreshColorPaletteOfAChart();
        }
        private void PieChartIncome_Loaded(object sender, RoutedEventArgs e)
        {
            pieChartIncome = (Chart) sender;
            refreshColorPaletteOfAChart(false);
        }
        private void LineChart_Loaded(object sender, RoutedEventArgs e)
        {
            lineChart = (Chart) sender;
        }

        /* REFRESHING */
        private void refreshColorPaletteOfAChart(bool expense=true)
        {
            List<Color> colors;
            if(expense)
                colors = (hubPageViewModels["RecordsViewModel"] as RecordsViewModel).ExpensesPerTagChartMap.Select(item => item.Color).ToList();
            else
                colors = (hubPageViewModels["RecordsViewModel"] as RecordsViewModel).IncomePerTagChartMap.Select(item => item.Color).ToList();
            if (colors.Count > 0)
            {
                var rdc = new ResourceDictionaryCollection();
                foreach (var color in colors)
                {
                    var rd = new ResourceDictionary();
                    var cb = new SolidColorBrush(color);
                    rd.Add("Background", cb);
                    Style pointStyle = new Style() {TargetType = typeof (Control)};
                    pointStyle.Setters.Add(new Setter(BackgroundProperty, cb));
                    Style shapeStyle = new Style() {TargetType = typeof (Shape)};
                    shapeStyle.Setters.Add(new Setter(Shape.StrokeProperty, cb));
                    shapeStyle.Setters.Add(new Setter(Shape.StrokeThicknessProperty, 2));
                    shapeStyle.Setters.Add(new Setter(Shape.StrokeMiterLimitProperty, 1));
                    shapeStyle.Setters.Add(new Setter(Shape.FillProperty, cb));
                    rd.Add("DataPointStyle", pointStyle);
                    rd.Add("DataShapeStyle", shapeStyle);
                    rdc.Add(rd);
                }
                if (expense)
                    pieChartExpenses.Palette = rdc;
                else
                    pieChartIncome.Palette = rdc;
            }
        }


        /* FIRST SECTION */ 
        private void AddExpense(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(NewExpensePage));
        }
        private void AddIncome(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(NewExpensePage), true);
        }
        private void AccountManagementButton_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (AccountManagementPage));
        }


        /* RECORDS SECTION */
        private void RecordsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var k = e.OriginalSource as ListView;
            object o = e.ClickedItem;
    	    ListViewItem lvi = (ListViewItem)k.ContainerFromItem(o);
    	    Grid grid = FindByName("RecordGrid", lvi) as Grid;
            grid.RowDefinitions[2].Height = (grid.RowDefinitions[2].Height==GridLength.Auto) ? new GridLength(0) : GridLength.Auto;
            grid.RowDefinitions[3].Height = (grid.RowDefinitions[3].Height==GridLength.Auto) ? new GridLength(0) : GridLength.Auto;
        }
        private FrameworkElement FindByName(string name, FrameworkElement root)
        {
            Stack<FrameworkElement> tree = new Stack<FrameworkElement>();
            tree.Push(root);

            while (tree.Count > 0)
            {
    	        FrameworkElement current = tree.Pop();
    	        if (current.Name == name)
    		        return current;

    	        int count = VisualTreeHelper.GetChildrenCount(current);
    	        for (int i = 0; i < count; ++i)
    	        {
    		        DependencyObject child = VisualTreeHelper.GetChild(current, i);
    		        if (child is FrameworkElement)
    			        tree.Push((FrameworkElement)child);
    	        }
            }

            return null;
        }

        private void ItemBorder_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FrameworkElement elem = sender as FrameworkElement;
            if (elem != null)
            {
                FlyoutBase.ShowAttachedFlyout(elem);
            }
        }
        private void RecordEdit_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                Frame.Navigate(typeof(NewExpensePage), menuFlItem.DataContext as Record);
            }
        }
        /* RECORD DELETE FLYOUT */
        private void RecordDelete_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.SetAttachedFlyout(RecordsHubSection, (Flyout)this.Resources["DeleteConfirmFlyout"]);
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                recordToDelete = menuFlItem.DataContext as Record;
                FlyoutBase.ShowAttachedFlyout(RecordsHubSection);
            }
        }
        private void RecordDeleteConfirmBtn_OnClick(object sender, RoutedEventArgs e)
        {
            (hubPageViewModels["RecordsViewModel"] as RecordsViewModel).DeleteRecord(recordToDelete);
            if(recordToDelete.Amount < 0)
                ((DataPointSeries) pieChartExpenses.Series[0]).ItemsSource = (hubPageViewModels["RecordsViewModel"] as RecordsViewModel).ExpensesPerTagChartMap;
            else
                ((DataPointSeries) pieChartIncome.Series[0]).ItemsSource = (hubPageViewModels["RecordsViewModel"] as RecordsViewModel).IncomePerTagChartMap;
            ((DataPointSeries) lineChart.Series[0]).ItemsSource = (hubPageViewModels["RecordsViewModel"] as RecordsViewModel).BalanceInTime;
            refreshColorPaletteOfAChart((recordToDelete.Amount < 0));
            /*var records = (hubPageViewModels["PendingRecordsViewModel"] as RecordsViewModel).Records;
            if(records.Count>0)
                records.Remove(records.First(x => x.ID==recordToDelete.ID));*/
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
        /* TAG DELETE FLYOUT */
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
            tagViewModel.DeleteTag(tagToDelete);
            //zkopírováno z NavigationHelper_LoadState ↑ todo: (bylo by vhodné zabalit do metody)
            (hubPageViewModels["RecordsViewModel"] as RecordsViewModel).GetFilteredRecords(filter);
            if(pieChartExpenses!=null)
                ((DataPointSeries) pieChartExpenses.Series[0]).ItemsSource = (hubPageViewModels["RecordsViewModel"] as RecordsViewModel).ExpensesPerTagChartMap;
            if(lineChart!=null)
                ((DataPointSeries) lineChart.Series[0]).ItemsSource = (hubPageViewModels["RecordsViewModel"] as RecordsViewModel).BalanceInTime;

            FlyoutBase.GetAttachedFlyout(TagHubSection).Hide();
        }

        private void TagDeleteCancelBtn_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(TagHubSection).Hide();
        }


        /* APPBAR BUTTONS */
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
        

        /* RECORD TRANSFER FLYOUT */
        private void RecordTransfer_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.SetAttachedFlyout(RecordsHubSection, (PickerFlyout)this.Resources["TransferPickerFlyout"]);
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                recordToTransfer = menuFlItem.DataContext as Record;
                FlyoutBase.ShowAttachedFlyout(RecordsHubSection);
            }
        }

        private void TransferPickerFlyout_OnOpening(object sender, object e)
        {
            var newAccountVM = new AccountsViewModel();
            newAccountVM.GetAccounts(true, recordToTransfer.Account.ID);
            this.hubPageViewModels["TransferRecord"] = recordToTransfer;
            this.hubPageViewModels["TransferAccounts"] = newAccountVM.Accounts;
        }

        private void TransferPickerFlyout_OnConfirmed(PickerFlyout sender, PickerConfirmedEventArgs args)
        {
            if (TransferPickerNewAccount.Items.Count > 0 && TransferPickerNewAccount.SelectedItem != null)
            {
                TransferPickerNoAccount.Visibility = Visibility.Collapsed;
                RecordsViewModel.TransferRecord(recordToTransfer, ((Account) TransferPickerNewAccount.SelectedItem).ID);
            }
            else
            {
                TransferPickerNoAccount.Visibility = Visibility.Visible;
            }
        }

        private void TransferPickerFlyout_OnOpened(object sender, object e)
        {
            if (TransferPickerNewAccount.Items.Count > 0)
                TransferPickerNewAccount.SelectedIndex = 0;
        }


        private void BilanceHide_Click(object sender, RoutedEventArgs e)
        {
            Grid grid = FindByName("BilanceGrid", RecordsHubSection) as Grid;
            grid.RowDefinitions[1].Height = (grid.RowDefinitions[1].Height==GridLength.Auto) ? new GridLength(0) : GridLength.Auto;
            grid.RowDefinitions[2].Height = (grid.RowDefinitions[2].Height==GridLength.Auto) ? new GridLength(0) : GridLength.Auto;
            grid.RowDefinitions[3].Height = (grid.RowDefinitions[3].Height==GridLength.Auto) ? new GridLength(0) : GridLength.Auto;
            if (grid.RowDefinitions[3].Height == new GridLength(0))
                (FindByName("BalanceTopCellTextBlock", grid) as TextBlock).Visibility = Visibility.Visible;
            else
                (FindByName("BalanceTopCellTextBlock", grid) as TextBlock).Visibility = Visibility.Collapsed;
        }


        private void PendingRecurrenceDisable_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                (hubPageViewModels["PendingRecordsViewModel"] as RecordsViewModel).DisableRecurrence((menuFlItem.DataContext as Record).RecurrenceChain.ID);
            }
        }


        private void FilterAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (FilterPage), filter);
        }
    }
}
