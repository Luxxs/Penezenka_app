using System;
using System.Collections.ObjectModel;
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

namespace Penezenka_App
{
    public sealed partial class NewTagPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary tagPageViewModel = new ObservableDictionary();
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
            if (e.NavigationParameter != null)
            {
                TagPageViewModel["Tag"] = (Tag) e.NavigationParameter;
                NewTagPageTitle.Visibility = Visibility.Collapsed;
                TagPageViewModel["SelectedColorItem"] = ((Tag) e.NavigationParameter).Color;
                editing = true;
            }
            else
            {
                EditTagPageTitle.Visibility = Visibility.Collapsed;
                TagPageViewModel["SelectedColorItem"] = new MyColors.ColorItem(MyColors.UIntColors[0], MyColors.ColorNames[0]);
            }

            ObservableCollection<MyColors.ColorItem> colors = new ObservableCollection<MyColors.ColorItem>();
            for (int i = 0; i < MyColors.UIntColors.Length; i++)
            {
                colors.Add(new MyColors.ColorItem(MyColors.UIntColors[i], MyColors.ColorNames[i]));
            }
            TagPageViewModel["ColorItems"] = colors;
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


        private void ColorGridView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                TagPageViewModel["SelectedColorItem"] = e.AddedItems[0];
            }
            FlyoutBase.GetAttachedFlyout(TagColorSelectButton).Hide();
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
            Frame.Navigate(typeof (HubPage));
        }

        private void CancelAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveAppBarButton.IsEnabled = false;
            CancelAppBarButton.IsEnabled = false;
            Frame.GoBack();
        }

        private void ColorPickerFlyout_Opening(object sender, object e)
        {
            TagPageCommandBar.Visibility = Visibility.Collapsed;
        }
        private void ColorPickerFlyout_Closed(object sender, object e)
        {
            TagPageCommandBar.Visibility = Visibility.Visible;
        }
    }
}
