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
using NMEAShared;

namespace NMEAReader
{
    public partial class IntroPage : PhoneApplicationPage
    {
        public string Version { get; private set; }


        public IntroPage()
        {
            InitializeComponent();
            Version = ApplicationSettingsHelper.Version();
            ContentPanel.DataContext = this;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void HyperlinkButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            EmailComposeTask email = new EmailComposeTask();
            email.To = "apps@craighorsfieldracing.com";
            email.Subject = "NMEA Reader, Windows Phone";
            email.Show();
        }
    }
}