using System;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;
using Penezenka_App.Common;
using Penezenka_App.Database;
using Penezenka_App.OtherClasses;
using Windows.ApplicationModel.Core;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.Activation;
using System.Collections.Generic;

namespace Penezenka_App
{
    public sealed partial class SettingsPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary settingsPageViewModel = new ObservableDictionary();
        private const string suggestedExportFileName = "Financni_zaznamnik_export";
        private ExportData importData;
        CoreApplicationView view;

        public SettingsPage()
        {
            this.InitializeComponent();
            view = CoreApplication.GetCurrentView();

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
        public ObservableDictionary SettingsPageViewModel
        {
            get { return this.settingsPageViewModel; }
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
            foreach (var key in AppSettings.Settings.Keys)
            {
                settingsPageViewModel[key] = AppSettings.Settings[key];
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

        private void SaveAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            bool correct = true;
            if (PasswordRequiredCheckBox.IsChecked.Value && string.IsNullOrEmpty(Password1.Password))
            {
                EmptyPasswordTextBlock.Visibility = Visibility.Visible;
                correct = false;
            }
            else
            {
                EmptyPasswordTextBlock.Visibility = Visibility.Collapsed;
            }
            if (PasswordRequiredCheckBox.IsChecked.Value && !Password1.Password.Equals(Password2.Password))
            {
                DifferentPasswordsTextBlock.Visibility = Visibility.Visible;
                correct = false;
            }
            else
            {
                DifferentPasswordsTextBlock.Visibility = Visibility.Collapsed;
            }

            if(correct)
            {
                SaveAppBarButton.IsEnabled = false;
                CancelAppBarButton.IsEnabled = false;
                AppSettings.SetPasswordRequired(PasswordRequiredCheckBox.IsChecked.Value);
                if(PasswordRequiredCheckBox.IsChecked.Value)
                    AppSettings.SetPassword(Password1.Password);
                Frame.Navigate(typeof(HubPage));
            }
        }

        private void CancelAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveAppBarButton.IsEnabled = false;
            CancelAppBarButton.IsEnabled = false;
            Frame.Navigate(typeof(HubPage));
        }

        private void ClearDatabaseButton_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void ClearDatabaseConfirmBtn_OnClick(object sender, RoutedEventArgs e)
        {
            DB.ClearTables();
            AllRecordsDeletedTextBlock.Visibility = Visibility.Visible;
            ExportDoneTextBlock.Visibility = Visibility.Collapsed;
            ExportFailedTextBlock.Visibility = Visibility.Collapsed;
            ImportDoneTextBlock.Visibility = Visibility.Collapsed;
            ImportFailedTextBlock.Visibility = Visibility.Collapsed;
            FlyoutBase.GetAttachedFlyout(ClearDatabaseButton).Hide();
        }

        private void ClearDatabaseCancelBtn_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(ClearDatabaseButton).Hide();
        }

        private void ExportToJson_OnClick(object sender, RoutedEventArgs e)
        {
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            fileSavePicker.SuggestedFileName = suggestedExportFileName;
            fileSavePicker.FileTypeChoices.Clear();
            fileSavePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });
            fileSavePicker.FileTypeChoices.Add("CSV", new List<string>() { ".csv" });
            fileSavePicker.PickSaveFileAndContinue();
            view.Activated += viewActivated;
        }

        private void ImportFromJson_OnClick(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            filePicker.ViewMode = PickerViewMode.List;
            filePicker.FileTypeFilter.Clear();
            filePicker.FileTypeFilter.Add(".json");
            filePicker.PickSingleFileAndContinue();
            view.Activated += viewActivated;
        }

        private async void viewActivated(CoreApplicationView sender, IActivatedEventArgs args1)
        {
            if (args1 != null)
            {
                if (args1 is FileOpenPickerContinuationEventArgs)
                {
                    var args = args1 as FileOpenPickerContinuationEventArgs;
                    if (args.Files.Count > 0)
                    {
                        try
                        {
                            var getData = Export.GetAllDataFromJson(args.Files[0]);
                            var exportData = DB.GetExportData();
                            importData = await getData;
                            ImportFailedTextBlock.Visibility = Visibility.Collapsed;
                            ImportDataFloutMessageTextBlock.Text = "Přejete si nahradit současná data v aplikaci (" + exportData.Count() + " položek) daty ze souboru (" + importData.Count() + " položek)?";
                            FlyoutBase.ShowAttachedFlyout(ImportFromJsonButton);
                        } catch(Exception ex)
                        {
                            ImportDoneTextBlock.Visibility = Visibility.Collapsed;
                            ImportFailedTextBlock.Text = "Import dat se nezdařil.";
                            if(ex.Message.Length < 700)
                                ImportFailedTextBlock.Text += " Podrobnosti:\n" +ex.Message;
                            ImportFailedTextBlock.Visibility = Visibility.Visible;
                        } finally
                        {
                            AllRecordsDeletedTextBlock.Visibility = Visibility.Collapsed;
                            ExportDoneTextBlock.Visibility = Visibility.Collapsed;
                            ExportFailedTextBlock.Visibility = Visibility.Collapsed;
                            view.Activated -= viewActivated;
                        }
                    }
                }
                else if(args1 is FileSavePickerContinuationEventArgs)
                {
                    var args = args1 as FileSavePickerContinuationEventArgs;
                    if(args.File != null)
                    {
                        try
                        {
                            int numLocalItems = 0;
                            if (args.File.ContentType.Equals("text/csv")) {
                                numLocalItems = await Export.ExportAllDataToCsv(args.File);
                            } else {
                                numLocalItems = await Export.SaveAllDataToJson(args.File);
                            }
                            ExportFailedTextBlock.Visibility = Visibility.Collapsed;
                            ExportDoneTextBlock.Text = "Export " + numLocalItems + " příjmů, výdajů, účtů a štítků proběhl úspěšně.";
                            ExportDoneTextBlock.Visibility = Visibility.Visible;
                        }
                        catch (Exception ex)
                        {
                            ExportDoneTextBlock.Visibility = Visibility.Collapsed;
                            ExportFailedTextBlock.Text = "Export dat se nezdařil.";
                            if (ex.Message.Length < 700)
                                ExportFailedTextBlock.Text += " Podrobnosti:\n" + ex.Message;
                            ExportFailedTextBlock.Visibility = Visibility.Visible;
                        }
                        finally
                        {
                            AllRecordsDeletedTextBlock.Visibility = Visibility.Collapsed;
                            ImportDoneTextBlock.Visibility = Visibility.Collapsed;
                            ImportFailedTextBlock.Visibility = Visibility.Collapsed;
                            view.Activated -= viewActivated;
                        }
                    }
                }
            }
        }

        private void ImportDataConfirmBtn_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(ImportFromJsonButton).Hide();
            Export.SaveExportedDataToDatabase(importData);
            ImportDoneTextBlock.Text = "Import " + importData.Count() + " příjmů, výdajů, účtů a štítků proběhl úspěšně.";
            ImportDoneTextBlock.Visibility = Visibility.Visible;
            App.Imported = true;
        }
        private void ImportDataCancelBtn_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(ImportFromJsonButton).Hide();
        }

        private void Flyout_Opened(object sender, object e)
        {
            if(ImportDataFloutMessageTextBlock.ActualHeight > 60)
            {
                ImportDataFloutMessageTextBlock.Margin = new Thickness(0);
            } else
            {
                ImportDataFloutMessageTextBlock.Margin = new Thickness(0,0,0,20);
            }
        }

        private void Hyperlink_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            FlyoutBase.ShowAttachedFlyout(ContentPanel);
        }

        private void Flyout_Closed(object sender, object e)
        {
            ImportDataFloutMessageTextBlock.Margin = new Thickness(0, 0, 0, 20);
        }
    }
}
