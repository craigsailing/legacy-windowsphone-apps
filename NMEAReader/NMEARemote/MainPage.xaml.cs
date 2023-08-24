using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using NMEARemote.Resources;
using NMEAShared;

namespace NMEARemote
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
                        
            BuildLocalizedApplicationBar();

            this.Loaded += MainPage_Loaded;
            
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApplicationSettingsHelper.IsFirstRun)
            {
                //Navigate to the Hello Page.
                NavigationService.Navigate(new Uri("/IntroPage.xaml", UriKind.Relative));
            }
        }

        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }
        
        #region App Bar
        private void BuildLocalizedApplicationBar()
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.
            ApplicationBar = new ApplicationBar();
            ApplicationBar.Mode = ApplicationBarMode.Minimized;

            // Create a new button and set the text value to the localized string from AppResources.
            ApplicationBarIconButton appBarButton = null;
            appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/feature.settings.png", UriKind.Relative));
            appBarButton.Text = AppResources.AppBarButtonSettingsText;
            appBarButton.Click += appBarButton_SettingsClick;
            ApplicationBar.Buttons.Add(appBarButton);
            
            appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/questionmark.png", UriKind.Relative));
            appBarButton.Text = AppResources.AppBarButtonAboutText;
            appBarButton.Click += appBarButton_AboutClick;
            ApplicationBar.Buttons.Add(appBarButton);
        }

        void appBarButton_AboutClick(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/IntroPage.xaml", UriKind.Relative));
        }

        void appBarButton_SettingsClick(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }
        #endregion
    }
}