using Penezenka_App;
using Penezenka_App.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage;
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
using WinRTXamlToolkit.Controls.Extensions;

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
        private TagViewModel tagViewModel = new TagViewModel();
        private ExportData importData;
        private bool imported;

        private RecordsViewModel.Filter filter = new RecordsViewModel.Filter
        {
            StartDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
            EndDateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1),
            AllTags = true,
            AllAccounts = true
        };
        private readonly SolidColorBrush buttonsDarkBackground = new SolidColorBrush(new Color{A=255, R=0x42, G=0x42, B=0x42});
        private readonly SolidColorBrush buttonsLightBackground = new SolidColorBrush(new Color{A=255, R=0xC7, G=0xC7, B=0xC7});
        private Record recordToDelete;
        private Chart pieChartExpenses;
        private Chart pieChartIncome;
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
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                hubPageViewModels["WalletsButtonImage"] = new BitmapImage(new Uri("ms-appx:///Assets/wallets_white.png"));
                hubPageViewModels["ButtonsBackground"] = buttonsDarkBackground;
            }
            else
            {
                hubPageViewModels["WalletsButtonImage"] = new BitmapImage(new Uri("ms-appx:///Assets/wallets.png"));
                hubPageViewModels["ButtonsBackground"] = buttonsLightBackground;
            }
            if (e.NavigationParameter != null && e.NavigationParameter is FileActivatedEventArgs && !imported)
            {
                var getData = Export.GetAllDataFromJSON((StorageFile)((FileActivatedEventArgs)e.NavigationParameter).Files[0]);
                var exportData = DB.GetExportData();
                int numLocalItems = exportData.Accounts.Count + exportData.RecurrenceChains.Count +
                                    exportData.Tags.Count +
                                    exportData.Records.Count - Export.ZeroIDRows;
                exportData = null;
                importData = await getData;
                int numFileItems = importData.Accounts.Count + importData.RecurrenceChains.Count + importData.Tags.Count +
                                   importData.Records.Count - Export.ZeroIDRows;
                ImportDataFloutMessageTextBlock.Text = "Přejete si nahradit současná data v aplikaci (" + numLocalItems +
                                                       " položek) daty ze souboru (" + numFileItems + " položek)?";
                FlyoutBase.SetAttachedFlyout(Hub, (Flyout)this.Resources["ImportDataMessageFlyout"]);
                if (Hub.IsInVisualTree())
                {
                    FlyoutBase.ShowAttachedFlyout(Hub);
                }
                else
                {
                    Hub.Loaded += Hub_Loaded;
                }
            }
            else if (e.NavigationParameter != null && e.NavigationParameter is RecordsViewModel.Filter)
                filter = e.NavigationParameter as RecordsViewModel.Filter;

            if (filter.StartDateTime == new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) &&
                filter.EndDateTime == new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1))
                RecordsHubSection.Header = "TENTO MĚSÍC";
            else
                RecordsHubSection.Header = "VYBRANÉ ZÁZNAMY";

            DB.AddRecurrentRecords();

            hubPageViewModels["RecordsViewModel"] = recordsViewModel;
            recordsViewModel.GetFilteredRecords(filter);

            pendingRecordsViewModel.GetRecurrentRecords(true);
            hubPageViewModels["PendingRecordsViewModel"] = pendingRecordsViewModel;

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
            FilterAppBarButton.IsEnabled = true;
            AddTagAppBarButton.IsEnabled = true;
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
            FlyoutBase.SetAttachedFlyout(Hub, (Flyout)this.Resources["AppExitConfirmFlyout"]);
            FlyoutBase.ShowAttachedFlyout(Hub);
        }
        private void AppExitConfirm_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(Hub).Hide();
            Application.Current.Exit();
        }
        private void AppExitCancel_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(Hub).Hide();
        }
        
        /* HUB CHANGES & LOADING */
        private void Hub_OnSectionsInViewChanged(object sender, SectionsInViewChangedEventArgs e)
        {
            string first, second, third, removed, added;
            //for debugging
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

        /* REFRESHING */
        private void refreshColorPaletteOfAChart(bool expense=true)
        {
            List<Color> colors;
            if(expense)
                colors = recordsViewModel.ExpensesPerTagChartMap.Select(item => item.Color).ToList();
            else
                colors = recordsViewModel.IncomePerTagChartMap.Select(item => item.Color).ToList();
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
            var record = (Record)e.ClickedItem;
    	    ListViewItem lvi = (ListViewItem)k.ContainerFromItem(record);
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
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                recordToDelete = menuFlItem.DataContext as Record;
                FlyoutBase.ShowAttachedFlyout(RecordsHubSection);
            }
        }
        private void RecordDeleteConfirmBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (recordsViewModel.DeleteRecord(recordToDelete))
            {
                pendingRecordsViewModel.Records.Remove(recordToDelete);
                // Mělo by aktualizovat položku ve výpise, když RecurrenceChain implementuje INotifyPropertyChanged, ne?
                foreach (var record in recordsViewModel.Records.Where(x => x.RecurrenceChain.ID == recordToDelete.RecurrenceChain.ID))
                {
                    record.RecurrenceChain.Disabled = true;
                }
            }

            refreshColorPaletteOfAChart((recordToDelete.Amount < 0));

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
        private void TagEdit_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                Tag tag = menuFlItem.DataContext as Tag;
                Frame.Navigate(typeof(NewTagPage), tag);
            }
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
            recordsViewModel.GetFilteredRecords(filter);

            refreshColorPaletteOfAChart(false);
            refreshColorPaletteOfAChart();

            pendingRecordsViewModel.GetRecurrentRecords(true);

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
            AddTagAppBarButton.IsEnabled = false;
            Frame.Navigate(typeof (NewTagPage));
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
            FilterAppBarButton.IsEnabled = false;
            Frame.Navigate(typeof (FilterPage), filter);
        }

        private void ChartsGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var tb = FindByName("ChartsLoadingTextBlock", NewButtonsHubSection) as TextBlock;
            if (tb != null)
            {
                tb.Visibility = Visibility.Collapsed;
            }
        }

        
        private void Hub_Loaded(object sender, RoutedEventArgs e)
        {
            if (importData != null)
            {
                FlyoutBase.ShowAttachedFlyout(Hub);
            }
        }
        private void ImportDataConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            Export.SaveExportedDataToDatabase(importData);
            recordsViewModel.GetFilteredRecords(filter);

            refreshColorPaletteOfAChart(false);
            refreshColorPaletteOfAChart();

            pendingRecordsViewModel.GetRecurrentRecords(true);

            tagViewModel.GetTags();
            importData = null;
            imported = true;
            FlyoutBase.GetAttachedFlyout(Hub).Hide();
        }
        private void ImportDataCancelBtn_Click(object sender, RoutedEventArgs e)
        {
            importData = null;
            imported = true;
            FlyoutBase.GetAttachedFlyout(Hub).Hide();
        }
    }
}
