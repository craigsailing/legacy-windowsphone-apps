using GribViewer.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using Windows.Phone.Storage.SharedAccess;
using Windows.Storage;

namespace GribViewer
{
    public partial class MainPage : PhoneApplicationPage
    {
        private bool _mapViewSet = false;
        private int FiltreLevel { get; set; }
        private MapLayer _windLayer = null;
        private DispatcherTimer _detailsTimer = null;
        private DispatcherTimer _forecastTimer = null;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public MainPage()
        {
            InitializeComponent();
            BuildLocalizedApplicationBar();
            FiltreLevel = 0;

            _windLayer = new MapLayer();

            _forecastTimer = new DispatcherTimer();
            _forecastTimer.Tick += ForecastTimer_Tick;
            
            // Set the data context of the listbox control to the sample data
            DataContext = App.Model;

            this.Loaded += MainPage_Loaded;
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApplicationSettingsHelper.IsFirstRun)
            {
                NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));

                StorageFolder isoStoreFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFolder appStoreFolder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Assets\\DemoData");
                StorageFile sourceFile = await appStoreFolder.GetFileAsync("1stgrib.grb");
                StorageFile copyFileOp = await sourceFile.CopyAsync(isoStoreFolder, "current.grb", NameCollisionOption.ReplaceExisting);
            }
            else
            {
                // Load data for the ViewModel Items
                if (!App.Model.IsDataLoaded)
                {
                    try
                    {
                        await semaphore.WaitAsync();
                        await App.Model.LoadData();   //TODO Move this to a background decode thread
                        semaphore.Release();
                        Analytics.LogEventWithMemmoryData("GribDecoded");
                    }
                    catch (InvalidDataException ex)
                    {
                        Analytics.LogEvent("GribDecodeFailed1");
                        Analytics.LogException("GribDecode", ex);
                        MessageBox.Show("The GRIB contained invalid data or data not supported in the current version. If you want you can email the GRIB to us and we will add support as soon as possible.");
                    }
                    catch (System.Exception ex)
                    {
                        Analytics.LogEvent("GribDecodeFailed2");
                        Analytics.LogException("GribDecode", ex);
                        MessageBox.Show("The GRIB contained invalid data or data not supported in the current version. If you want you can email the GRIB to us and we will add support as soon as possible.");
                    }
                }
                if (App.Model.IsDataLoaded && _mapViewSet == false)
                {
                    //Only set the view on first constuct and navigate.
                    _mapViewSet = true;

                    //Set the map to the GRIB
                    if (App.Model.Center != null)
                        GribMap.SetView(App.Model.Center, App.Model.MapZoom, MapAnimationKind.None);
                    else
                        GribMap.SetView(new LocationRectangle(App.Model.GribCenter, App.Model.GridWidth, App.Model.GridHight), MapAnimationKind.None);

                    //Add the initial wind data to the map
                    GenerateBarbData2(0);
                    AppBarNextBackEnabled();
                }

                if (SystemTray.ProgressIndicator != null)
                    SystemTray.ProgressIndicator.IsVisible = false;
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //If there is a inbound grib shared with the application copy this to the isostore for loading
            //TODO we need to bring up the progess indicator here maybe it could be a slow copy
            if (NavigationContext.QueryString.ContainsKey("fileId"))
            {
                string fileId = NavigationContext.QueryString["fileId"];

                if (string.IsNullOrWhiteSpace(fileId))
                {
                    MessageBox.Show("No file path was provided, error!");
                    Analytics.LogEvent("LoadNewGribNoPathError");
                    return;
                }

                await semaphore.WaitAsync();
                await SharedStorageAccessManager.CopySharedFileAsync(ApplicationData.Current.LocalFolder, "current.grb", NameCollisionOption.ReplaceExisting, fileId);
                Analytics.LogEvent("LoadedNewGrib");
                semaphore.Release();
            }
            Analytics.LogEventWithMemmoryData("MainPageViewed");
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {

        }

        private void GenerateBarbData2(double resolution)
        {
            System.Diagnostics.Debug.WriteLine("GenerateBarbData2");

            List<long> ticks = new List<long>();
            ticks.Add(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            if (App.Model.IsDataLoaded)
            {

                GribMap.Layers.Clear();
                //GribMap.Layers.Remove(_windLayer);
                ticks.Add(ticks[0] - (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond));

                _windLayer.Clear();                     //Do you need to clear this to prevent a leak
                _windLayer = null;
                _windLayer = new MapLayer();            //Note on Win Phone 8 this results in a crash if a new collection is not used
                ticks.Add(ticks[0] - (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond));

                WindLayer layer = WindLayer.Instance();
                ticks.Add(ticks[0] - (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond));

                foreach (MapOverlay overlay in layer.LayerView(resolution))
                {
                    if (!_windLayer.Contains(overlay))
                    {
                        _windLayer.Add(overlay);
                    }
                }
                ticks.Add(ticks[0] - (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond));

                GribMap.Layers.Add(_windLayer);

                ticks.Add(ticks[0] - (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond));
            }
        }
        
        #region App Bar

        private void BuildLocalizedApplicationBar()
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.
            ApplicationBar = new ApplicationBar();
            //ApplicationBar.Mode = ApplicationBarMode.Minimized;
            ApplicationBar.Opacity = 0.25;

            // Create a new button and set the text value to the localized string from AppResources.
            ApplicationBarIconButton appBarButton = null;
            appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/download.png", UriKind.Relative));
            appBarButton.Text = AppResources.AppBarButtonDownload;
            appBarButton.Click += appBarButton_DownloadClick;
            ApplicationBar.Buttons.Add(appBarButton);

            appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/back.png", UriKind.Relative));
            appBarButton.Text = AppResources.AppBarButtonBack;
            appBarButton.Click += appBarButton_BackClick;
            appBarButton.IsEnabled = false;
            ApplicationBar.Buttons.Add(appBarButton);

            appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/next.png", UriKind.Relative));
            appBarButton.Text = AppResources.AppBarButtonNext;
            appBarButton.Click += appBarButton_NextClick;
            appBarButton.IsEnabled = false;
            ApplicationBar.Buttons.Add(appBarButton);

            appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/transport.play.png", UriKind.Relative));
            appBarButton.Text = AppResources.AppBarButtonPlay;
            appBarButton.Click += appBarButton_PlayClick;
            appBarButton.IsEnabled = false;
            //ApplicationBar.Buttons.Add(appBarButton);

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

        void appBarButton_DownloadClick(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }

        void appBarButton_PlayClick(object sender, EventArgs e)
        {
            //Anomate time steps
        }

        void appBarButton_BackClick(object sender, EventArgs e)
        {
            if (_forecastTimer.IsEnabled)
            {
                return;
            }

            //Go back 1 time step
            if (App.Model.CurrentForecastInterval == 0)
            {
                //At the start cant go back.
                return;
            }

            try
            {
                Busy(true);
                --App.Model.CurrentForecastInterval;
                AppBarNextBackEnabled();
                GenerateBarbData2(App.Model.MapDisplayResolution);
            }
            finally
            {
                //Busy(false);
            }
        }

        void appBarButton_NextClick(object sender, EventArgs e)
        {
            if (_forecastTimer.IsEnabled)
            {
                return;
            }

            //Go to the net time step
            try
            {
                Busy(true);
                ++App.Model.CurrentForecastInterval;
                AppBarNextBackEnabled();
                GenerateBarbData2(App.Model.MapDisplayResolution);
            }
            finally
            {
                //Busy(false);
            }
        }

        private void AppBarNextBackEnabled()
        {

            //At the Max Interval
            if (App.Model.CurrentForecastInterval == (App.Model.IntervalMax - 1))
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
            }
            else
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = true;
            }

            if (App.Model.CurrentForecastInterval > 0)
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = true;
            }
            else
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
            }

            //There is only 1 interval
            if (App.Model.IntervalMax == 1)
            {
                ((ApplicationBarIconButton)ApplicationBar.Buttons[2]).IsEnabled = false;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;
            }
        }

        #endregion

        private void GribMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = "NEED STOR APP ID";
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = "AUTH TOKEN NEEDED";

            //Load Grib Data now if needed.
            if (App.Model.IsDataLoaded)
            {
                //Load data in a Map Layer
            }
        }

        private void GribMap_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //Get Information at this location for the layers that are displayed
            GeoCoordinate xy = null;
            try
            {
                Point p = e.GetPosition(GribMap);
                xy = this.GribMap.ConvertViewportPointToGeoCoordinate(p);

            }
            catch (SystemException)
            {
                //if you cant convert the location just return from the handler
                return;
            }

            //Update the text?
            double windSpeed = 0;
            int windAngle = 0;
            double pressure = 0;
            WindLayer layerWind = WindLayer.Instance();
            PressureLayer layerPressure = PressureLayer.Instance();
            
            if (layerWind.DataAtPoint(ref xy, ref windSpeed, ref windAngle))
            {
                //Update the text
                DataItem1.Text = string.Format("{0}: {1:F1}kt, {2}°", AppResources.Wind, windSpeed, windAngle);

                if (layerPressure.DataAtPoint(xy, ref pressure))
                {
                    DataItem1.Text += string.Format("\n{0}: {1:F0}mb", AppResources.Pressure, pressure / 100);
                }

#if DEBUG
                DataItem1.Text += string.Format("\n{0}: {1:F0}mb", "Curr Mem", DeviceStatus.ApplicationCurrentMemoryUsage / 1000000);
                DataItem1.Text += string.Format("\n{0}: {1:F0}mb", "Peak Mem", DeviceStatus.ApplicationPeakMemoryUsage / 1000000);
#endif

                //Store the last requested information point
                App.Model.PosLastInfo = xy;

                //Update the fade remove timer
                DataPanel.Visibility = System.Windows.Visibility.Visible;

                StartDetailsTimer();
            }

            e.Handled = true;
        }

        private void GribMap_ZoomLevelChanged(object sender, Microsoft.Phone.Maps.Controls.MapZoomLevelChangedEventArgs e)
        {
            if (App.Model.IsDataLoaded)
            {
                //Store Map settings
                double prevZoomlevel = App.Model.MapZoom;
                App.Model.MapZoom = GribMap.ZoomLevel;
                App.Model.Pos0 = GribMap.ConvertViewportPointToGeoCoordinate(new Point(0, 0));
                App.Model.Pos1 = GribMap.ConvertViewportPointToGeoCoordinate(new Point(GribMap.ActualWidth, GribMap.ActualHeight));
                App.Model.Center = GribMap.Center;

                #region first level attempt
                
                //Zooming out and passed 9 level change Res
                if (GribMap.ZoomLevel < 9.0 && prevZoomlevel >= 9.0)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format("  GribMap_ZoomLevelChanged: Zoom: {0}", GribMap.ZoomLevel.ToString()));
                    //0.25 to 0.5
                    App.Model.MapDisplayResolution = 0.25;

                    //Remove a layers
                    GenerateBarbData2(App.Model.MapDisplayResolution);
                    return;
                }

                //Zooming out and passed 7 level change Res
                if (GribMap.ZoomLevel < 7.0 && prevZoomlevel >= 7.0)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format("  GribMap_ZoomLevelChanged: Zoom: {0}", GribMap.ZoomLevel.ToString()));
                    //0.25 to 0.5
                    App.Model.MapDisplayResolution = 0.5;

                    //Remove a layers
                    GenerateBarbData2(App.Model.MapDisplayResolution);
                    return;
                }

                //Zooming out and passed 6 level change Res
                if (GribMap.ZoomLevel < 6.0 && prevZoomlevel >= 6.0)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format("  GribMap_ZoomLevelChanged: Zoom: {0}", GribMap.ZoomLevel.ToString()));
                    //0.5 to 1
                    App.Model.MapDisplayResolution = 1;

                    //Remove a layers
                    GenerateBarbData2(App.Model.MapDisplayResolution);
                    return;
                }

                //Zooming out and passed 5 level change Res
                if (GribMap.ZoomLevel < 5 && prevZoomlevel >= 5)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format("  GribMap_ZoomLevelChanged: Zoom: {0}", GribMap.ZoomLevel.ToString()));
                    //1 to 2 deg res
                    App.Model.MapDisplayResolution = 2;

                    //Remove a layer
                    GenerateBarbData2(App.Model.MapDisplayResolution);
                    return;
                }

                //Zooming out and passed 4 level change Res
                if (GribMap.ZoomLevel < 4 && prevZoomlevel >= 4)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format("  GribMap_ZoomLevelChanged: Zoom: {0}", GribMap.ZoomLevel.ToString()));
                    //1 to 2 deg res
                    App.Model.MapDisplayResolution = 4;

                    //Remove a layer
                    GenerateBarbData2(App.Model.MapDisplayResolution);
                    return;
                }

                //Zooming in and passed 9 level change Res
                if (GribMap.ZoomLevel > 9.5 && prevZoomlevel <= 9.5)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format("  GribMap_ZoomLevelChanged: Zoom: {0}", GribMap.ZoomLevel.ToString()));
                    //0.5 to 1
                    App.Model.MapDisplayResolution = 0.125;

                    //Remove a layer
                    GenerateBarbData2(App.Model.MapDisplayResolution);
                    return;
                }

                //Zooming in and passed 7 level change Res
                if (GribMap.ZoomLevel > 7.5 && prevZoomlevel <= 7.5)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format("  GribMap_ZoomLevelChanged: Zoom: {0}", GribMap.ZoomLevel.ToString()));
                    //0.5 to 1
                    App.Model.MapDisplayResolution = 0.25;

                    //Remove a layer
                    GenerateBarbData2(App.Model.MapDisplayResolution);
                    return;
                }

                //Zooming in and passed 6 level change Res
                if (GribMap.ZoomLevel > 6.5 && prevZoomlevel <= 6.5)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format("  GribMap_ZoomLevelChanged: Zoom: {0}", GribMap.ZoomLevel.ToString()));
                    //0.5 to 1
                    App.Model.MapDisplayResolution = 0.5;

                    //Remove a layer
                    GenerateBarbData2(App.Model.MapDisplayResolution);
                    return;
                }

                //Zooming in and passed 5 level change Res
                if (GribMap.ZoomLevel > 5.5 && prevZoomlevel <= 5.5)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format("  GribMap_ZoomLevelChanged: Zoom: {0}", GribMap.ZoomLevel.ToString()));
                    //1 to 2 deg res
                    App.Model.MapDisplayResolution = 1;

                    //Remove a layer
                    GenerateBarbData2(App.Model.MapDisplayResolution);
                    return;
                }


                //Zooming in and passed 4 level change Res
                if (GribMap.ZoomLevel > 4.5 && prevZoomlevel <= 4.5)
                {
                    //System.Diagnostics.Debug.WriteLine(string.Format("  GribMap_ZoomLevelChanged: Zoom: {0}", GribMap.ZoomLevel.ToString()));
                    //1 to 2 deg res
                    App.Model.MapDisplayResolution = 2;

                    //Remove a layer
                    GenerateBarbData2(App.Model.MapDisplayResolution);
                    return;
                }

                #endregion
            }
        }

        private void GribMap_CenterChanged(object sender, Microsoft.Phone.Maps.Controls.MapCenterChangedEventArgs e)
        {
            App.Model.Pos0 = GribMap.ConvertViewportPointToGeoCoordinate(new Point(0, 0));
            App.Model.Pos1 = GribMap.ConvertViewportPointToGeoCoordinate(new Point(GribMap.ActualWidth, GribMap.ActualHeight));
            App.Model.MapZoom = GribMap.ZoomLevel;
            App.Model.Center = GribMap.Center;
        }

        private void GribMap_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            //Use this event for Pan and Zoom as we only want to adjust at the end of the manipulation
        }

        private void Busy(bool busy)
        {
            if (busy)
            {
                GribMap.IsEnabled = false;  
                SystemTray.ProgressIndicator.IsVisible = busy;
                SystemTray.ProgressIndicator.Text = "Loading ...";
                StartProgressTimer();
            }
            else
            {
                //SystemTray.ProgressIndicator.IsVisible = busy;
                //GribMap.IsEnabled = true;
            }
        }

        private void StartProgressTimer()
        {
            _forecastTimer.Stop();
            _forecastTimer.Interval = new TimeSpan(0, 0, 0, 1, 500);
            _forecastTimer.Start();
        }

        private void ForecastTimer_Tick(object sender, EventArgs e)
        {
            if (_forecastTimer != null)
            {
                _forecastTimer.Stop();

                if (SystemTray.ProgressIndicator != null)
                    SystemTray.ProgressIndicator.IsVisible = false;
                
                GribMap.IsEnabled = true;
            }
        }

        private void StartDetailsTimer()
        {
            //Delay allocation the timer and only create it on first use.
            if (_detailsTimer == null)
            {
                _detailsTimer = new DispatcherTimer();
                _detailsTimer.Tick += DetailsTimer_Tick;
            }

            //Start timer
            _detailsTimer.Stop();
            _detailsTimer.Interval = new TimeSpan(0, 0, 6);
            _detailsTimer.Start();
        }

        private void DetailsTimer_Tick(object sender, EventArgs e)
        {
            if (_detailsTimer != null)
            {
                //Hide the details UI
                _detailsTimer.Stop();
                DataPanel.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void DataPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/DataTable.xaml", UriKind.Relative));
        }

    }
}