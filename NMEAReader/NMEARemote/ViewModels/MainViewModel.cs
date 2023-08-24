using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using NMEARemote.Resources;
using NMEAShared;
using System.Windows;

namespace NMEARemote.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private NMEAData _nmeaData = null; 

        public MainViewModel()
        {
            this._nmeaData = new NMEAData();

            AllData = _nmeaData.NMEADataPoints;
            Speed = new ObservableCollection<NMEADataItem>(from nMEADataItem in AllData where (nMEADataItem.Group & NMEADataItem.GROUP.Speed) ==  NMEADataItem.GROUP.Speed select nMEADataItem);
            Wind = new ObservableCollection<NMEADataItem>(from nMEADataItem in AllData where (nMEADataItem.Group & NMEADataItem.GROUP.Wind) == NMEADataItem.GROUP.Wind select nMEADataItem);
            Navigation = new ObservableCollection<NMEADataItem>(from nMEADataItem in AllData where (nMEADataItem.Group & NMEADataItem.GROUP.Navigation) == NMEADataItem.GROUP.Navigation  select nMEADataItem);
            Helm = new ObservableCollection<NMEADataItem>(from nMEADataItem in AllData where (nMEADataItem.Group & NMEADataItem.GROUP.Helm) == NMEADataItem.GROUP.Helm select nMEADataItem);
            Favourites = new ObservableCollection<NMEADataItem>(from nMEADataItem in AllData where (nMEADataItem.Group & NMEADataItem.GROUP.Favourites) == NMEADataItem.GROUP.Favourites select nMEADataItem);

            //Have the models events pass to the ViewModel ? Not sure this is a good idea?
            //_nmeaData.PropertyChanged += (sender, args) => this.NotifyPropertyChanged(args.PropertyName);
            _nmeaData.PropertyChanged += this.PropertyChangeRouter;
        }

        public void Disconnect()
        {
            _nmeaData.Disconnect();
        }

        public bool Connecting
        {
            get { return _nmeaData.Connecting; }
        }

        public bool Connected
        {
            get { return _nmeaData.Connected; }
        }

        //Collections used in each Pivot Item
        public ObservableCollection<NMEADataItem> AllData { get; private set; }
        public ObservableCollection<NMEADataItem> Speed { get; private set; }
        public ObservableCollection<NMEADataItem> Wind { get; private set; }
        public ObservableCollection<NMEADataItem> Helm { get; private set; }
        public ObservableCollection<NMEADataItem> Navigation { get; private set; }
        public ObservableCollection<NMEADataItem> Favourites { get; private set; }

        public NMEAData NMEADataChannel 
        {
            get { return this._nmeaData; }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public async void LoadData()
        {
            bool wasConnected = false;
            ApplicationSettingsHelper.TryGetSetting<bool>("WasRunningAtExit", out wasConnected);
            if (wasConnected)
            {
                try
                {
                    await this._nmeaData.ConnectAsync();
                }
                catch(System.Exception)
                {
                    //Dont Fail app start if we cant reconnect just message that to the user.
                    MessageBox.Show(string.Format("Error: {0}\nVerify the NMEA data is available", _nmeaData.SocketErrorInformation), "Connection Error", MessageBoxButton.OK);
                }
            }
            this.IsDataLoaded = true;
        }

        #region INotifyPropertyChange 
        public event PropertyChangedEventHandler PropertyChanged;
        
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Used to map some of the model events to the view if needed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void PropertyChangeRouter(object sender, PropertyChangedEventArgs args)
        {
            bool routeEvent = false;

            if ( string.Compare(args.PropertyName, "Connecting", StringComparison.InvariantCultureIgnoreCase )  == 0 )
                routeEvent = true;

            //Route only events we want to from the model to the view.
            if (routeEvent == true)
                this.NotifyPropertyChanged(args.PropertyName);
        }
        #endregion

    }
    
}