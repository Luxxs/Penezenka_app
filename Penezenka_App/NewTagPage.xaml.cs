using Windows.Phone.UI.Input;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using Penezenka_App.Common;
using Penezenka_App.Model;
using Penezenka_App.OtherClasses;
using Penezenka_App.ViewModel;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media;

namespace Penezenka_App
{
    public sealed partial class NewTagPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary tagPageViewModel = new ObservableDictionary();
        private RecordsViewModel.Filter filter;
        private bool editing = false;

        public NewTagPage()
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
        public ObservableDictionary TagPageViewModel
        {
            get { return this.tagPageViewModel; }
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
                EditTagPageTitle.Visibility = Visibility.Collapsed;
                TagPageViewModel["SelectedColorItem"] = new MyColors.ColorItem(MyColors.UIntColors[0], MyColors.ColorNames[0]);
            }
            else
            {
                TagPageViewModel["Tag"] = TagViewModel.GetTagByID(navigationParam.Id);
                NewTagPageTitle.Visibility = Visibility.Collapsed;
                TagPageViewModel["SelectedColorItem"] = ((Tag)TagPageViewModel["Tag"]).Color;
                editing = true;
            }
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

        private void TagColorSelectButton_OnTapped(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element != null)
            {
                FlyoutBase.ShowAttachedFlyout(element);
            }
        }


        private void SaveAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveAppBarButton.IsEnabled = false;
            CancelAppBarButton.IsEnabled = false;
            Color color = ((MyColors.ColorItem) TagPageViewModel["SelectedColorItem"]).Color;
            if (editing)
                TagViewModel.UpdateTag(((Tag)TagPageViewModel["Tag"]).ID, TagTitle.Text, color, TagNotes.Text);
            else
                TagViewModel.InsertTag(TagTitle.Text, color, TagNotes.Text);
            Frame.Navigate(typeof (HubPage), Export.SerializeObjectToJsonString<RecordsViewModel.Filter>(filter));
        }

        private void CancelAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveAppBarButton.IsEnabled = false;
            CancelAppBarButton.IsEnabled = false;
            Frame.GoBack();
        }

        private void ColorPickerFlyout_Opened(object sender, object e)
        {
            TagPageCommandBar.Visibility = Visibility.Collapsed;
            double width = ((ColorsGrid.Children[0] as StackPanel).Children[0] as Rectangle).ActualWidth;
            foreach (StackPanel stackPanel in ColorsGrid.Children)
            {
                foreach(Rectangle rect in stackPanel.Children)
                {
                    rect.Height = width;
                }
            }
        }
        private void ColorPickerFlyout_Closed(object sender, object e)
        {
            TagPageCommandBar.Visibility = Visibility.Visible;
        }

        private void Rectangle_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            TagPageViewModel["SelectedColorItem"] = new MyColors.ColorItem(((sender as Rectangle).Fill as SolidColorBrush).Color);
            FlyoutBase.GetAttachedFlyout(TagColorSelectButton).Hide();
        }
    }
}
