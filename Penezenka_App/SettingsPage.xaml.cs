﻿using System;
using System.IO;
using Windows.Phone.UI.Input;
using Windows.Storage;
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
            /*settingsPageViewModel["path"] = KnownFolders.DocumentsLibrary.DisplayName + "/" + exportDataFilename;
            ExportImportPathInfoTextBlock.Text += settingsPageViewModel["path"];
            FileNotFoundTextBlock.Text = "Soubor " + settingsPageViewModel["path"] + " nenalezen.";*/
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
            fileSavePicker.SuggestedFileName = "Penezenka_data_export";
            fileSavePicker.FileTypeChoices.Clear();
            fileSavePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });
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
                        var getData = Export.GetAllDataFromJson(args.Files[0]);
                        FileNotFoundTextBlock.Visibility = Visibility.Collapsed;
                        var exportData = DB.GetExportData();
                        importData = await getData;
                        ImportDataFloutMessageTextBlock.Text = "Přejete si nahradit současná data v aplikaci (" + exportData.Count() + " položek) daty ze souboru (" + importData.Count() + " položek)?";

                        view.Activated -= viewActivated;
                        FlyoutBase.ShowAttachedFlyout(ImportFromJsonButton);
                    }
                }
                else if(args1 is FileSavePickerContinuationEventArgs)
                {
                    var args = args1 as FileSavePickerContinuationEventArgs;
                    if(args.File != null)
                    {
                        int numLocalItems = await Export.SaveAllDataToJson(args.File);
                        ExportDoneTextBlock.Text = "Export " + numLocalItems + " položek proběhl úspěšně.";
                        ExportDoneTextBlock.Visibility = Visibility.Visible;
                        view.Activated -= viewActivated;
                    }
                }
            }
        }

        private void ImportDataConfirmBtn_OnClick(object sender, RoutedEventArgs e)
        {
            Export.SaveExportedDataToDatabase(importData);
            ImportDoneTextBlock.Text = "Import " + importData.Count() + " položek proběhl úspěšně.";
            ImportDoneTextBlock.Visibility = Visibility.Visible;
            FlyoutBase.GetAttachedFlyout(ImportFromJsonButton).Hide();
        }

        private void ImportDataCancelBtn_OnClick(object sender, RoutedEventArgs e)
        {
            FlyoutBase.GetAttachedFlyout(ImportFromJsonButton).Hide();
        }
    }
}
