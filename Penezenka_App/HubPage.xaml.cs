﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
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

        private RecordFilter filter;
        private readonly SolidColorBrush LoadingBarGridDarkBackground = new SolidColorBrush(new Color{A=127, R=0, G=0, B=0});
        private readonly SolidColorBrush LoadingBarGridLightBackground = new SolidColorBrush(new Color{A=127, R=255, G=255, B=255});
        private Record recordToDelete;
        private Chart pieChartExpenses;
        private Chart pieChartIncome;
        private Tag tagToDelete;
        private bool searchConfirmed = false;

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
                hubPageViewModels["WalletsButtonImage"] = new BitmapImage(new Uri("ms-appx:///Assets/wallets2.1_white.png"));
                hubPageViewModels["LoadingBarGridBackground"] = LoadingBarGridDarkBackground;
            }
            else
            {
                hubPageViewModels["WalletsButtonImage"] = new BitmapImage(new Uri("ms-appx:///Assets/wallets2.1.png"));
                hubPageViewModels["LoadingBarGridBackground"] = LoadingBarGridLightBackground;
            }


            DB.AddRecurrentRecords();
            hubPageViewModels["RecordsViewModel"] = recordsViewModel;

            if (e.NavigationParameter is string && !string.IsNullOrEmpty(e.NavigationParameter as string))
            {
                filter = Export.DeserializeObjectFromJsonString<RecordFilter>(e.NavigationParameter as string);
                if(filter.IsDefault)
                {
                    SetDefaultDate();
                }
            }
            filter = filter ?? RecordFilter.Default;
            if(filter.IsDefault && SearchTextBox != null && !string.IsNullOrEmpty(SearchTextBox.Text))
            {
                GetFoundRecords(false);
            } else
            {
                ClearSearch();
                recordsViewModel.GetFilteredRecords(filter);
            }
            SetRecordsHubSectionHeader();
            GetRecordsChartsDataIfVisible();
            
            hubPageViewModels["SortingMethods"] = new List<string>()
            {
                "podle data (od nejnovějšího)",
                "podle data (od nejstaršího)",
                "podle částky (od největší)",
                "podle částky (od nejmenší)",
                "podle názvu (a → z)",
                "podle názvu (z → a)"
            };

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
            var showChartsGrid = FindByName("ShowChartsGrid", ChartsHubSection) as Grid;
            var chartsScrollViewer = FindByName("ChartsScrollViewer", ChartsHubSection) as ScrollViewer;
            if (showChartsGrid != null && chartsScrollViewer != null)
            {
                showChartsGrid.Visibility = Visibility.Visible;
                chartsScrollViewer.Visibility = Visibility.Collapsed;
            }
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

        #region OnLoaded functions
        private void PieChartExpenses_OnLoaded(object sender, RoutedEventArgs e)
        {
            pieChartExpenses = (Chart)sender;
            RefreshColorPaletteOfAChart();
        }
        private void PieChartIncome_OnLoaded(object sender, RoutedEventArgs e)
        {
            pieChartIncome = (Chart)sender;
            RefreshColorPaletteOfAChart(false);
        }
        private void ChartsGrid_OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadingBarGrid.Visibility = Visibility.Collapsed;
        }
        #endregion

        private void SetDefaultDate()
        {
            var maxDate = RecordsViewModel.GetMaxDate();
            var now = DateTimeOffset.Now;
            filter = new RecordFilter();
            filter.IsDefault = true;
            if (maxDate.Year < now.Year || (maxDate.Year == now.Year && maxDate.Month < now.Month))
            {
                filter.SetMonth(maxDate);
            }
            else
            {
                filter.SetMonth(now);
            }
        }

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
                SortAppBarButton.Visibility = Visibility.Visible;
                buttonVisible = true;
            }
            else if(e.RemovedSections.Count > 0 && Hub.SectionsInView[0].Name.Equals("ChartsHubSection") ||
                e.AddedSections.Count > 0 && Hub.SectionsInView[0].Name.Equals("RecordsHubSection") && e.AddedSections[0].Name.Equals("RecurrenceHubSection"))
            {
                FilterAppBarButton.Visibility = Visibility.Visible;
                SearchAppBarButton.Visibility = Visibility.Collapsed;
                SortAppBarButton.Visibility = Visibility.Collapsed;
                buttonVisible = true;
            } else
            {
                FilterAppBarButton.Visibility = Visibility.Collapsed;
                SearchAppBarButton.Visibility = Visibility.Collapsed;
                SortAppBarButton.Visibility = Visibility.Collapsed;
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

        /* REFRESHING */
        private void RefreshColorPaletteOfAChart(bool expense=true)
        {
            if (expense && pieChartExpenses != null || !expense && pieChartIncome != null)
            {
                List<Color> colors;
                if (expense)
                    colors = recordsViewModel.ExpensePerTags.Select(item => item.Color).ToList();
                else
                    colors = recordsViewModel.IncomePerTags.Select(item => item.Color).ToList();
                if (colors.Count > 0)
                {
                    var rdc = new ResourceDictionaryCollection();
                    foreach (var color in colors)
                    {
                        var rd = new ResourceDictionary();
                        var cb = new SolidColorBrush(color);
                        rd.Add("Background", cb);
                        Style pointStyle = new Style() {TargetType = typeof(Control)};
                        pointStyle.Setters.Add(new Setter(BackgroundProperty, cb));
                        Style shapeStyle = new Style() {TargetType = typeof(Shape)};
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
        }


        /* FIRST SECTION */
        #region First section
        private void AddExpense(object sender, RoutedEventArgs e)
        {
            var parameter = new IdFilterPair
            {
                Id = -1,
                Filter = filter
            };
            Frame.Navigate(typeof(NewRecordPage), Export.SerializeObjectToJsonString<IdFilterPair>(parameter));
        }
        private void AddIncome(object sender, RoutedEventArgs e)
        {
            var parameter = new IdFilterPair
            {
                Id = 0,
                Filter = filter
            };
            Frame.Navigate(typeof(NewRecordPage), Export.SerializeObjectToJsonString<IdFilterPair>(parameter));
        }
        private void AccountManagementButton_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (AccountManagementPage));
        }
        #endregion


        /* RECORDS SECTION */
        #region Records section
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
                var parameter = new IdFilterPair
                {
                    Id = (menuFlItem.DataContext as Record).ID,
                    Filter = filter
                };
                Frame.Navigate(typeof(NewRecordPage), Export.SerializeObjectToJsonString<IdFilterPair>(parameter));
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
                ReloadRecordsListView(recordToDelete.RecurrenceChain.ID);
            }

            RefreshColorPaletteOfAChart(recordToDelete.Amount < 0);

            FlyoutBase.GetAttachedFlyout(RecordsHubSection).Hide();
        }
        private void RecordDeleteCancelBtn_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(RecordsHubSection).Hide();
        }

        private void ReloadRecordsListView(int recurrenceChainId)
        {
            bool disabledRecurrenceInTheList = false;
            foreach (var record in recordsViewModel.Records.Where(x => x.RecurrenceChain.ID == recurrenceChainId))
            {
                record.RecurrenceChain.Disabled = true;
                disabledRecurrenceInTheList = true;
            }
            if (disabledRecurrenceInTheList)
            {
                var recordsListView = (ListView)FindByName("RecordsListView", RecordsHubSection);
                var source = recordsListView.ItemsSource;
                recordsListView.ItemsSource = null;
                recordsListView.ItemsSource = source;
            }
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
                (FindByName("BilanceButton", grid) as TextBlock).Text = "Bilance ↑";
            }
            else
            {
                (FindByName("BalanceTopCellTextBlock", grid) as TextBlock).Visibility = Visibility.Collapsed;
                (FindByName("BilanceButton", grid) as TextBlock).Text = "Bilance ↓";
            }
        }

        private void SetRecordsHubSectionHeader()
        {
            if (searchConfirmed)
            {
                RecordsHubSection.Header = "VYHLEDANÉ ZÁZNAMY";
            }
            else if (filter.IsMonth(DateTimeOffset.Now))
            {
                RecordsHubSection.Header = "TENTO MĚSÍC";
            }
            else if (filter.IsMonth(RecordsViewModel.GetMaxDate()))
            {
                RecordsHubSection.Header = "POSLEDNÍ MĚSÍC ZÁZNAMŮ";
            }
            else
            {
                RecordsHubSection.Header = "VYBRANÉ ZÁZNAMY";
            }
        }
        #endregion


        /* CHARTS SECTION */
        #region Charts section
        private void DisplayLineChartButton_Click(object sender, RoutedEventArgs e)
        {
            DisplayLineChart();
        }

        private void ShowChartsButton_Click(object sender, RoutedEventArgs e)
        {
            var showChartsGrid = FindByName("ShowChartsGrid", ChartsHubSection) as Grid;
            var chartsScrollViewer = FindByName("ChartsScrollViewer", ChartsHubSection) as ScrollViewer;
            showChartsGrid.Visibility = Visibility.Collapsed;
            chartsScrollViewer.Visibility = Visibility.Visible;
            GetRecordsChartsDataIfVisible();
            DisplayLineChart();
        }

        private void DisplayLineChart()
        {
            var lineChart = FindByName("LineChart", ChartsHubSection) as Chart;
            var button = FindByName("DisplayLineChartButton", ChartsHubSection) as Button;
            if (lineChart != null && button != null)
            {
                lineChart.Visibility = Visibility.Visible;
                button.Visibility = Visibility.Collapsed;
            }
        }

        private void GetRecordsChartsDataIfVisible()
        {
            var chartsScrollViewer = FindByName("ChartsScrollViewer", ChartsHubSection) as ScrollViewer;
            if (chartsScrollViewer == null || // in case of first loading of the page (after app launch)
                chartsScrollViewer.Visibility == Visibility.Visible)
            {
                recordsViewModel.GetGroupedRecordsPerTag();
                recordsViewModel.GetGroupedRecordsPerTag(true);
                recordsViewModel.GetBalanceInTime();
                RefreshColorPaletteOfAChart();
                RefreshColorPaletteOfAChart(false);
            }
        }
        #endregion


        /* PENDING RECURRENT RECORDS SECTION */
        #region Pending recurrent records section
        private void PendingRecurrenceDisable_OnClick(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                pendingRecordsViewModel.DisableRecurrence((menuFlItem.DataContext as Record).RecurrenceChain.ID);
                ReloadRecordsListView((menuFlItem.DataContext as Record).RecurrenceChain.ID);
            }
        }
        #endregion


        /* TAGS SECTION */
        #region Tags section
        private void TagsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var parameter = new IdFilterPair
            {
                Id = (e.ClickedItem as Tag).ID,
                Filter = filter
            };
            Frame.Navigate(typeof(NewTagPage), Export.SerializeObjectToJsonString<IdFilterPair>(parameter));
        }
        private void TagEdit_OnClick(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem menuFlItem = sender as MenuFlyoutItem;
            if (menuFlItem != null && menuFlItem.DataContext != null)
            {
                Tag tag = menuFlItem.DataContext as Tag;
                var parameter = new IdFilterPair
                {
                    Id = tag.ID,
                    Filter = filter
                };
                Frame.Navigate(typeof(NewTagPage), Export.SerializeObjectToJsonString<IdFilterPair>(parameter));
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
            if (searchConfirmed)
            {
                ConfirmSearch();
            }
            else
            {
                recordsViewModel.GetFilteredRecords(filter);
                GetRecordsChartsDataIfVisible();
            }

            pendingRecordsViewModel.GetRecurrentRecords(true);

            FlyoutBase.GetAttachedFlyout(TagHubSection).Hide();
        }

        private void TagDeleteCancelBtn_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(TagHubSection).Hide();
        }
        #endregion


        /* APPBAR BUTTONS */
        #region AppBarButtons
        private void Settings_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (SettingsPage), Export.SerializeObjectToJsonString<RecordFilter>(filter));
        }
        private void About_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof (AboutPage));
        }
        
        private void FilterAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            FilterAppBarButton.IsEnabled = false;
            Frame.Navigate(typeof (FilterPage), Export.SerializeObjectToJsonString<RecordFilter>(filter));
        }
        private void AddTagAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            AddTagAppBarButton.IsEnabled = false;
            var parameter = new IdFilterPair
            {
                Id = 0,
                Filter = filter
            };
            Frame.Navigate(typeof (NewTagPage), Export.SerializeObjectToJsonString<IdFilterPair>(parameter));
        }
        #endregion


        /* SEARCH FLYOUT */
        #region Search
        private void SearchAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.SetAttachedFlyout(RecordsHubSection, (PickerFlyout)this.Resources["SearchRecordsFlyout"]);
            FlyoutBase.ShowAttachedFlyout(RecordsHubSection);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GetFoundRecords(true);
        }
        private void SearchWhereChB_Changed(object sender, RoutedEventArgs e)
        {
            GetFoundRecords(true);
        }
        private void SearchTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Accept)
            {
                ConfirmSearch();
            }
        }
        private void SearchFlyout_OnConfirmed(PickerFlyout sender, PickerConfirmedEventArgs args)
        {
            ConfirmSearch();
        }
        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSearch();
            recordsViewModel.GetFilteredRecords(filter);
            GetRecordsChartsDataIfVisible();
        }

        private void ConfirmSearch()
        {
            if (string.IsNullOrEmpty(SearchTextBox.Text))
            {
                ClearSearch();
                recordsViewModel.GetFilteredRecords(filter);
                GetRecordsChartsDataIfVisible();
            }
            else
            {
                FlyoutBase.GetAttachedFlyout(RecordsHubSection).Hide();
                GetFoundRecords(false);
                GetRecordsChartsDataIfVisible();
                searchConfirmed = true;
                SetRecordsHubSectionHeader();
            }
        }
        private void ClearSearch()
        {
            searchConfirmed = false;
            SearchTextBox.Text = string.Empty;
            FlyoutBase.GetAttachedFlyout(RecordsHubSection).Hide();
            SetRecordsHubSectionHeader();
        }
        private void GetFoundRecords(bool onlyCount)
        {
            if (SearchInTitleChB != null && SearchInNotesChB != null && SearchInAllChB != null && SearchInFilteredChB != null && SearchInDisplayedChB != null)
            {
                RecordSearchArea area;
                if (SearchInAllChB.IsChecked.Value)
                    area = RecordSearchArea.All;
                else if (SearchInFilteredChB.IsChecked.Value)
                    area = RecordSearchArea.Filter;
                else
                    area = RecordSearchArea.Displayed;
                recordsViewModel.GetSearchedRecords(SearchTextBox.Text, SearchInTitleChB.IsChecked.Value, SearchInNotesChB.IsChecked.Value, area, onlyCount);
            }
        }
        #endregion

        /* SORTING */
        #region Sorting
        private void SortAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement) sender);
        }
        private void SortListPicker_Opened(object sender, object e)
        {
            (sender as ListPickerFlyout).SelectedValue = ((sender as ListPickerFlyout).ItemsSource as List<string>)[(recordsViewModel.RecordsSorting ==-1) ? 0 : recordsViewModel.RecordsSorting];
        }
        private void SortListPicker_ItemsPicked(ListPickerFlyout sender, ItemsPickedEventArgs args)
        {
            if (args.AddedItems.Count > 0)
            {
                recordsViewModel.RecordsSorting = sender.SelectedIndex;
            }
        }
        #endregion

        #region AppExit functions
        private void AppExitConfirm_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(Hub).Hide();
            Application.Current.Exit();
        }
        private void AppExitCancel_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(Hub).Hide();
        }
        #endregion
    }
}
