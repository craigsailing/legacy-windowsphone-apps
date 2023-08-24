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

namespace GribViewer
{
    public partial class About : PhoneApplicationPage
    {
        public string Version { get; private set; }
        public About()
        {
            InitializeComponent();

            Version = ApplicationSettingsHelper.Version();
            ContentPanel.DataContext = this;
        }

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            //Navigate back to the calling page
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void HyperlinkButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            EmailComposeTask email = new EmailComposeTask();
            email.To = "apps@craighorsfieldracing.com";
            email.Subject = "Grib Viewer, Windows Phone " + Version;
            email.Show();
        }
    }
}