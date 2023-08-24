using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using NMEAShared;

namespace NMEARemote
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

        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void HyperlinkButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            EmailComposeTask email = new EmailComposeTask();
            email.To = "apps@craighorsfieldracing.com";
            email.Subject = "NMEA Remote, Windows Phone";
            email.Show();
        }
    }
}