using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Activation;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Penezenka_App.Common;
using Penezenka_App.Database;
using Penezenka_App.Model;
using Penezenka_App.OtherClasses;
using Penezenka_App.ViewModel;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using WinRTXamlToolkit.Controls.Extensions;

// The Hub Application template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace Penezenka_App
{
    public sealed partial class HubPage : Page
    {
        private readonly NavigationHelper navigationHelper;
        private readonly ObservableDictionary hubPageViewModels = new ObservableDictionary();
        private RecordsViewModel recordsViewModel = new RecordsViewModel();
        private RecordsViewModel pendingRecordsViewModel = new RecordsViewModel();
        private TagViewModel tagViewModel = new TagViewModel();

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
            if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                hubPageViewModels["WalletsButtonImage"] = new BitmapImage(new Uri("ms-appx:///Assets/wallets2_white.png"));
                hubPageViewModels["ButtonsBackground"] = buttonsDarkBackground;
            }
            else
            {
                hubPageViewModels["WalletsButtonImage"] = new BitmapImage(new Uri("ms-appx:///Assets/wallets2.png"));
                hubPageViewModels["ButtonsBackground"] = buttonsLightBackground;
            }

            DB.AddRecurrentRecords();
            hubPageViewModels["RecordsViewModel"] = recordsViewModel;

            if (e.NavigationParameter is string && !string.IsNullOrEmpty(e.NavigationParameter as string))
            {
                filter = (RecordsViewModel.Filter)Export.DeserializeObjectFromJsonString((e.NavigationParameter as string),
                    typeof (RecordsViewModel.Filter));
                recordsViewModel.GetFilteredRecords(filter);
            }
            else if(SearchTextBox != null && !string.IsNullOrEmpty(SearchTextBox.Text))
            {
                GetFoundRecords(false);
            }
            else
            {
                recordsViewModel.GetFilteredRecords(filter);
            }

            if (filter.StartDateTime == new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) &&
                filter.EndDateTime == new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1))
            {
                RecordsHubSection.Header = "TENTO MĚSÍC";
            }
            else
            {
                RecordsHubSection.Header = "VYBRANÉ ZÁZNAMY";
            }

            pendingRecordsViewModel.GetRecurrentRecords(true);
            hubPageViewModels["PendingRecordsViewModel"] = pendingRecordsViewModel;

            tagViewModel.GetTags();
            this.hubPageViewModels["Tags"] = tagViewModel.Tags;
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
            FilterAppBarButton.IsEnabled = true;
            AddTagAppBarButton.IsEnabled = true;
            if(Frame.BackStack.Count > 0 && Frame.BackStack[0].SourcePageType == typeof(LoginPage))
            {
                Frame.BackStack.RemoveAt(0);
            }
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
            e.Handled = true;
            try {
                FlyoutBase.ShowAttachedFlyout(Hub);
            } catch(Exception) { }
        }
        private void AppExitConfirm_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(Hub).Hide();
            Application.Current.Exit();
        }
        private void AppExitCancel_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(Hub).Hide();
        }
        
        /* HUB CHANGES & LOADING */
        private void Hub_OnSectionsInViewChanged(object sender, SectionsInViewChangedEventArgs e)
        {
            bool buttonVisible = false;
            if(e.RemovedSections.Count > 0 && Hub.SectionsInView[0].Name.Equals("TagHubSection") ||
               e.AddedSections.Count > 0 && e.AddedSections[0].Name.Equals("NewButtonsHubSection") && Hub.SectionsInView[0].Name.Equals("RecurrenceHubSection"))
            {
                AddTagAppBarButton.Visibility = Visibility.Visible;
                buttonVisible = true;
            }
            else
            {
                AddTagAppBarButton.Visibility = Visibility.Collapsed;
            }
            if(e.RemovedSections.Count > 0 && Hub.SectionsInView[0].Name.Equals("RecordsHubSection") ||
               e.AddedSections.Count > 0 && Hub.SectionsInView[0].Name.Equals("NewButtonsHubSection") && e.AddedSections[0].Name.Equals("ChartsHubSection"))
            {
                FilterAppBarButton.Visibility = Visibility.Visible;
                SearchAppBarButton.Visibility = Visibility.Visible;
                buttonVisible = true;
            }
            else if(e.RemovedSections.Count > 0 && Hub.SectionsInView[0].Name.Equals("ChartsHubSection") ||
                e.AddedSections.Count > 0 && Hub.SectionsInView[0].Name.Equals("RecordsHubSection") && e.AddedSections[0].Name.Equals("RecurrenceHubSection"))
            {
                FilterAppBarButton.Visibility = Visibility.Visible;
                SearchAppBarButton.Visibility = Visibility.Collapsed;
                buttonVisible = true;
            } else
            {
                FilterAppBarButton.Visibility = Visibility.Collapsed;
                SearchAppBarButton.Visibility = Visibility.Collapsed;

            }

            if (buttonVisible)
            {
                HubPageCommandBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
            }
            else
            {
                HubPageCommandBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
            }

        }
        private void PieChartExpenses_OnLoaded(object sender, RoutedEventArgs e)
        {
            pieChartExpenses = (Chart) sender;
            RefreshColorPaletteOfAChart();
        }
        private void PieChartIncome_OnLoaded(object sender, RoutedEventArgs e)
        {
            pieChartIncome = (Chart) sender;
            RefreshColorPaletteOfAChart(false);
        }
        private void ChartsGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadingBarGrid.Visibility = Visibility.Collapsed;
        }

        /* REFRESHING */
        private void RefreshColorPaletteOfAChart(bool expense=true)
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
            Frame.Navigate(typeof(NewRecordPage));
        }
        private void AddIncome(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(NewRecordPage), true);
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

        // The (probable) original method is avaliable at: http://stackoverflow.com/questions/7034522/how-to-find-element-in-visual-tree-wp7
        // Author of the original version: E.Z. Hart
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
        private void RecordEdit_OnClick(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                Frame.Navigate(typeof(NewRecordPage), (menuFlItem.DataContext as Record).ID);
            }
        }
        // Record delete flyouts
        private void RecordDelete_OnClick(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                FlyoutBase.SetAttachedFlyout(RecordsHubSection, (Flyout)this.Resources["RecordDeleteFlyout"]);
                recordToDelete = menuFlItem.DataContext as Record;
                FlyoutBase.ShowAttachedFlyout(RecordsHubSection);
            }
        }
        private void RecordDeleteConfirmBtn_OnClick(object sender, RoutedEventArgs e)
        {
            if (recordsViewModel.DeleteRecord(recordToDelete))
            {
                pendingRecordsViewModel.Records.Remove(recordToDelete);
                foreach (var record in recordsViewModel.Records.Where(x => x.RecurrenceChain.ID == recordToDelete.RecurrenceChain.ID))
                {
                    record.RecurrenceChain.Disabled = true;
                }
            }

            RefreshColorPaletteOfAChart((recordToDelete.Amount < 0));

            FlyoutBase.GetAttachedFlyout(RecordsHubSection).Hide();
        }
        private void RecordDeleteCancelBtn_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(RecordsHubSection).Hide();
        }

        private void BilanceHide_OnClick(object sender, RoutedEventArgs e)
        {
            Grid grid = FindByName("BilanceGrid", RecordsHubSection) as Grid;
            grid.RowDefinitions[1].Height = (grid.RowDefinitions[1].Height==GridLength.Auto) ? new GridLength(0) : GridLength.Auto;
            grid.RowDefinitions[2].Height = (grid.RowDefinitions[2].Height==GridLength.Auto) ? new GridLength(0) : GridLength.Auto;
            grid.RowDefinitions[3].Height = (grid.RowDefinitions[3].Height==GridLength.Auto) ? new GridLength(0) : GridLength.Auto;
            if (grid.RowDefinitions[3].Height == new GridLength(0))
            {
                (FindByName("BalanceTopCellTextBlock", grid) as TextBlock).Visibility = Visibility.Visible;
                (FindByName("BilanceButton", grid) as HyperlinkButton).Content = "Bilance ↑";
            }
            else
            {
                (FindByName("BalanceTopCellTextBlock", grid) as TextBlock).Visibility = Visibility.Collapsed;
                (FindByName("BilanceButton", grid) as HyperlinkButton).Content = "Bilance ↓";
            }
        }
        
        /* PENDING RECURRENT RECORDS SECTION */
        private void PendingRecurrenceDisable_OnClick(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                pendingRecordsViewModel.DisableRecurrence((menuFlItem.DataContext as Record).RecurrenceChain.ID);
            }
        }

        /* TAGS SECTION */
        private void TagsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(NewTagPage), (e.ClickedItem as Tag).ID);
        }
        private void TagEdit_OnClick(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                Tag tag = menuFlItem.DataContext as Tag;
                Frame.Navigate(typeof(NewTagPage), tag.ID);
            }
        }
        // Tag delete flyouts
        private void TagDelete_OnClick(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                tagToDelete = menuFlItem.DataContext as Tag;
                FlyoutBase.ShowAttachedFlyout(TagHubSection);
            }
        }
        private void TagDeleteConfirmBtn_OnClick(object sender, RoutedEventArgs e)
        {
            tagViewModel.DeleteTag(tagToDelete);
            recordsViewModel.GetFilteredRecords(filter);

            RefreshColorPaletteOfAChart(false);
            RefreshColorPaletteOfAChart();

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
        
        private void FilterAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            FilterAppBarButton.IsEnabled = false;
            Frame.Navigate(typeof (FilterPage), Export.SerializeObjectToJsonString(filter, typeof(RecordsViewModel.Filter)));
        }
        private void AddTagAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            AddTagAppBarButton.IsEnabled = false;
            Frame.Navigate(typeof (NewTagPage));
        }
        

        /* SEARCH FLYOUT */
        private void FindAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.SetAttachedFlyout(RecordsHubSection, (PickerFlyout)this.Resources["SearchRecordsFlyout"]);
            FlyoutBase.ShowAttachedFlyout(RecordsHubSection);
        }
        private void SearchFlyout_OnConfirmed(PickerFlyout sender, PickerConfirmedEventArgs args)
        {
            if (!string.IsNullOrEmpty(SearchTextBox.Text))
                GetFoundRecords(false);
            else
                recordsViewModel.GetFilteredRecords(filter);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetFoundRecords(true);
        }
        private void SearchWhereChB_Changed(object sender, RoutedEventArgs e)
        {
            GetFoundRecords(true);
        }
        private void GetFoundRecords(bool onlyCount)
        {
            if (SearchInTitleChB != null && SearchInNotesChB != null && SearchInAllChB != null && SearchInFilteredChB != null && SearchInDisplayedChB != null)
            {
                RecordSearchArea area;
                if (SearchInAllChB.IsChecked.Value)
                    area = RecordSearchArea.ALL;
                else if (SearchInFilteredChB.IsChecked.Value)
                    area = RecordSearchArea.FILTER;
                else
                    area = RecordSearchArea.DISPLAYED;
                recordsViewModel.GetSearchedRecords(SearchTextBox.Text, SearchInTitleChB.IsChecked.Value, SearchInNotesChB.IsChecked.Value, area, onlyCount);
                if (!onlyCount)
                {
                    RefreshColorPaletteOfAChart();
                    RefreshColorPaletteOfAChart(true);
                }
            }
        }
        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            recordsViewModel.GetFilteredRecords(filter);
        }

        private void SearchTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Accept)
            {
                FlyoutBase.GetAttachedFlyout(RecordsHubSection).Hide();
                GetFoundRecords(false);
            }
        }
    }
}
