using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Windows.Networking.Proximity;
using NMEAShared;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Shell;
using NMEARemote.Resources;
using System;

namespace NMEARemote
{
public partial class SettingsPage : PhoneApplicationPage
    {
        private NMEAData _nmeaData;

        // Constructor
        public SettingsPage()
        {
            InitializeComponent();
            BuildLocalizedApplicationBar();

            if (_nmeaData == null)
                _nmeaData = App.ViewModel.NMEADataChannel;

            DataContext = this._nmeaData;

            this.Loaded += MainPage_Loaded;
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_nmeaData.Mode == NMEAData.ConnectionMode.BlueTooth)
            {
                this.RadioBT.IsChecked = true;
            }
            else if (_nmeaData.Mode == NMEAData.ConnectionMode.TCP)
            {
                this.RadioTCP.IsChecked = true; ;
            }
            else if (_nmeaData.Mode == NMEAData.ConnectionMode.UDP)
            {
                this.RadioUDP.IsChecked = true;
            }
            else
            {
                this.DeviceListPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.ServerPort.Visibility = System.Windows.Visibility.Collapsed;
                this.Server.Visibility = System.Windows.Visibility.Collapsed;
                this.BtnRefresh.Visibility = System.Windows.Visibility.Collapsed;
            }

            if (this._nmeaData.Connected && !this._nmeaData.Connecting)
            {
                //If startup the page and it is already connected change the mode to connected and alow disconnect.
                this.BtnConnect.Content =  AppResources.BtnDisconnect;
                this.BtnConnect.IsEnabled = true;
            }
            else
            {
                this.BtnConnect.IsEnabled = false;
            }
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_nmeaData.Connected == false)
            {
                try
                {
                    if (this._nmeaData.Mode == NMEAData.ConnectionMode.DEMO) { }

                    if ((this._nmeaData.Mode == NMEAData.ConnectionMode.UDP 
                        || this._nmeaData.Mode == NMEAData.ConnectionMode.TCP) 
                        && string.IsNullOrEmpty(this._nmeaData.PortNumber))
                    {
                        MessageBox.Show("Port number has to be set. Many devices use port 10110 for NMEA.", "Invalid Port Number", MessageBoxButton.OK);
                        return;
                    }

                    if ( this._nmeaData.Mode == NMEAData.ConnectionMode.TCP 
                        && string.IsNullOrEmpty(this._nmeaData.ServerNameIP) )
                    {
                        MessageBox.Show("Server Name or IP has to be set.", "Invalid Server Name", MessageBoxButton.OK);
                        return;
                    }

                    //Try to connect to the stream now.
                    this.BtnConnect.IsEnabled = false;
                    await _nmeaData.ConnectAsync();

                    //If you can connect allow this to be disconected
                    this.BtnConnect.Content =  AppResources.BtnDisconnect;
                    Analytics.LogEvent("ConnectedToData");
                }
                catch (System.Exception ex)
                {
                    //Failed to Connect.
                    Analytics.LogEvent("ConnectToDataFailed");
                    Analytics.LogException(_nmeaData.SocketErrorInformation, ex);
                    MessageBox.Show(string.Format("Error: {0}\nVerify the NMEA data is available", _nmeaData.SocketErrorInformation), "Connection Error", MessageBoxButton.OK);
                }
                finally
                {
                    this.BtnConnect.IsEnabled = true;
                }
            }
            else
            {
                _nmeaData.Disconnect();
                this.BtnConnect.Content =  AppResources.BtnConnect;
            }
        }
        
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await this._nmeaData.RefeshBlueToothDevicesAsync();
        }
                
        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 1) 
            {
                //Count is 1 if the item is selected 0 on unselect use this to avoid call on load and unload
                //Item is selected ...
                _nmeaData.CurrentDevice = (PeerInformation)DeviceList.SelectedItem;
                this.BtnConnect.IsEnabled = true;
            }
            
            if (DeviceList.SelectedItems.Count == 0)
                this.BtnConnect.IsEnabled = false;
        }

        private void RadioUDP_Checked(object sender, RoutedEventArgs e)
        {
            if (this._nmeaData != null )
            {
                CheckWiFi();
                this._nmeaData.Mode = NMEAData.ConnectionMode.UDP;
                this.DeviceList.IsEnabled = false;
                this.DeviceListPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.ServerPort.Visibility = System.Windows.Visibility.Visible;
                this.Server.Visibility = System.Windows.Visibility.Collapsed;
                this.BtnRefresh.Visibility = System.Windows.Visibility.Collapsed;
                this.BtnConnect.IsEnabled = true;
            }
        }

        private void RadioTCP_Checked(object sender, RoutedEventArgs e)
        {
            if (this._nmeaData != null)
            {
                CheckWiFi();
                this._nmeaData.Mode = NMEAData.ConnectionMode.TCP;
                this.DeviceList.IsEnabled = false;
                this.DeviceListPanel.Visibility = System.Windows.Visibility.Collapsed;
                this.ServerPort.Visibility = System.Windows.Visibility.Visible;
                this.Server.Visibility = System.Windows.Visibility.Visible;
                this.BtnRefresh.Visibility = System.Windows.Visibility.Collapsed;
                this.BtnConnect.IsEnabled = true;
            }
        }
        
        private void RadioBT_Checked(object sender, RoutedEventArgs e)
        {
            if (this._nmeaData != null)
            {
                this._nmeaData.Mode = NMEAData.ConnectionMode.BlueTooth;

                this.BtnRefresh.Visibility = System.Windows.Visibility.Visible;
                this.ServerPort.Visibility = System.Windows.Visibility.Collapsed;
                this.DeviceListPanel.Visibility = System.Windows.Visibility.Visible;
                this.DeviceList.IsEnabled = true;

                if (DeviceList.SelectedIndex == -1)
                    this.BtnConnect.IsEnabled = false;
            }
        }

        private void CheckWiFi()
        {
            if (NetworkInterface.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
            {
                MessageBox.Show("The phone must be on joined to a WiFi network for this mode.", "WiFi", MessageBoxButton.OK);
            }
        }
    
        private void BtnDemo_Click(object sender, RoutedEventArgs e)
        {
            //Run In Demo Mode.
            this._nmeaData.Mode = NMEAData.ConnectionMode.DEMO;

            this.DeviceList.IsEnabled = false;
            this.DeviceListPanel.Visibility = System.Windows.Visibility.Collapsed;
            this.ServerPort.Visibility = System.Windows.Visibility.Collapsed;
            this.Server.Visibility = System.Windows.Visibility.Collapsed;
            this.BtnRefresh.Visibility = System.Windows.Visibility.Collapsed;

            BtnConnect_Click(sender, e);
            Analytics.LogEvent("Running_DemoMode");
        }

        private void BuildLocalizedApplicationBar()
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.
            ApplicationBar = new ApplicationBar();
            ApplicationBar.Mode = ApplicationBarMode.Default;

            // Create a new button and set the text value to the localized string from AppResources.
            ApplicationBarIconButton appBarButton = null;

            appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/questionmark.png", UriKind.Relative));
            appBarButton.Text = AppResources.AppBarButtonAboutText;
            appBarButton.Click += appBarButton_AboutClick;
            ApplicationBar.Buttons.Add(appBarButton);
        }

        void appBarButton_AboutClick(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/IntroPage.xaml", UriKind.Relative));
        }
    }
}