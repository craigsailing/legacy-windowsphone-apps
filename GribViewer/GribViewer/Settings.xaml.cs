using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using GribViewer.Resources;

namespace GribViewer
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();
            BuildLocalizedApplicationBar();

            // Set the data context of the listbox control to the sample data
            DataContext = App.Model;

            this.Loaded += Settings_Loaded;
        }

        void Settings_Loaded(object sender, RoutedEventArgs e) { }

        private void Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (ApplicationSettingsHelper.IsFirstEmail)
            {
                MessageBox.Show(AppResources.EmailSignature);
            }

            //Request a grib from the current provider
            if (App.Model.Pos0 == null || App.Model.Pos1 == null)
            {
                //Error Select a valid map area
                MessageBox.Show(AppResources.MapError1);
                return;
            }

            //saildocs if via email
            EmailComposeTask email = new EmailComposeTask();
            email.To = "query@saildocs.com";
            email.Subject = "Request Grib";
            email.Body = App.Model.EmailQueryString();

            if (email.Body != string.Empty)
            {
                Analytics.LogEvent("GribEmailRequest");
                email.Show();
            }

            //ugrib via web service todo
        }

        private void ForcastDuration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Need to snap a value to a integer only support full days.
            if (ForcastDuration != null)
            {
                ForcastDuration.Value = Math.Round(ForcastDuration.Value);
            }
        }

        private void ForecastInterval_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ForecastInterval != null)
            {
                //3
                if (ForecastInterval.Value < 4.5)
                {
                    ForecastInterval.Value = 3;
                    return;
                }

                //6
                if (ForecastInterval.Value <= 9)
                {
                    ForecastInterval.Value = 6;
                    return;
                }

                //12
                if (ForecastInterval.Value < 16)
                {
                    ForecastInterval.Value = 12;
                    return;
                }

                //24
                if (ForecastInterval.Value >= 16)
                {
                    ForecastInterval.Value = 24;
                    return;
                }

            }
        }

        private void ForecastRes_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Need to snap a value to a integer only support full days.
            if (ForecastRes != null)
            {
                //0.5 1 2

                if (ForecastRes.Value < 0.35)
                {
                    ForecastRes.Value = 0.25;
                    return;
                }

                if (ForecastRes.Value < 0.75)
                {
                    ForecastRes.Value = 0.5;
                    return;
                }

                //0.5 1 2
                if (ForecastRes.Value < 1.5)
                {
                    ForecastRes.Value = 1;
                    return;
                }

                //0.5 1 2
                if (ForecastRes.Value > 1.5)
                {
                    ForecastRes.Value = 2;
                    return;
                }

            }
        }

        private void BuildLocalizedApplicationBar()
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.
            ApplicationBar = new ApplicationBar();
            //ApplicationBar.Mode = ApplicationBarMode.Minimized;
            //ApplicationBar.Opacity = 0.25;

            // Create a new button and set the text value to the localized string from AppResources.
            ApplicationBarIconButton appBarButton = null;
            appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/questionmark.png", UriKind.Relative));
            appBarButton.Text = AppResources.AppBarButtonAbout;
            appBarButton.Click += appBarButton_AboutClick;
            ApplicationBar.Buttons.Add(appBarButton);
        }

        private void appBarButton_AboutClick(object sender, EventArgs e)
        {
            //Navigate to the About Page. 
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
        }

    }
}