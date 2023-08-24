using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace NMEAShared
{
    public class NMEAData : INotifyPropertyChanged, IDisposable
    {
        private bool disposed = false;
        private static readonly uint ReadBufferSize = 128;      //NMEA sentance is max 82 chars at 4800 baud this is max 6 sentances (Note this)

        public enum ConnectionMode
        {
            BlueTooth,
            UDP,
            TCP,
            DEMO,
        };

        #region Members
        
        private string _dataString = string.Empty;
        private bool _connectedSate = false;
        private bool _connecting = false;
        private bool _loggingEnabled = false;
        private bool _runUnderLock = false;
        private StreamSocket _readSocket = null;
        private DatagramSocket _readUDPSocket = null;
        private DataReader _dataReader = null;
        private Thread _pollingThread = null;
        private string _portNumber;
        private string _serverNameIP;
        private ConnectionMode _connectionMode;
        private PeerInformation _currentDevice;
        public ObservableCollection<PeerInformation> _bTDevices = null;
        public ObservableCollection<NMEADataItem> _nmeaData = null;

        #endregion 

        #region .ctor

        public NMEAData() 
        {
            Init();
        }

        private async void Init()
        {
            Connecting = false;
            Connected = false;
            SocketErrorInformation = string.Empty;

            _bTDevices = new ObservableCollection<PeerInformation>();
            _nmeaData = new ObservableCollection<NMEADataItem>();

            NMEADataItem.BuildDataCollection(ref _nmeaData);

            //Restore Data now
            bool runUnderLock = false;
            this.RunUnderLock = runUnderLock;
            if (ApplicationSettingsHelper.TryGetSetting<bool>("RunUnderLock", out runUnderLock))
                this.RunUnderLock = runUnderLock;

            string serverIP = string.Empty;
            if (ApplicationSettingsHelper.TryGetSetting<string>("ServerIP", out serverIP))
                ServerNameIP = serverIP;

            string portNumber = string.Empty;
            if (ApplicationSettingsHelper.TryGetSetting<string>("PortNumber", out portNumber))
                PortNumber = portNumber;
            else
                PortNumber = "10110";

            ConnectionMode mode;
            if (ApplicationSettingsHelper.TryGetSetting<ConnectionMode>("Mode", out mode))
                Mode = mode;
                        
            string btDeviceName = string.Empty;
            if (Mode == ConnectionMode.BlueTooth && ApplicationSettingsHelper.TryGetSetting("Device", out btDeviceName))
            {
                //refresh the bt devices and select this one 
                await RefeshBlueToothDevicesAsync();
                foreach (PeerInformation device in _bTDevices)
                {
                    if (string.Compare(device.DisplayName, btDeviceName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        CurrentDevice = device;
                    }
                }
            }
        }

        #endregion

        #region Properties
                
        public PeerInformation CurrentDevice
        {
            get { return _currentDevice; }
            set
            {
                if (_currentDevice != value)
                {
                    _currentDevice = value;
                    this.NotifyPropertyChanged();
                    ApplicationSettingsHelper.StoreSetting("Device", _currentDevice.DisplayName);
                }
            }
        }

        public string SocketErrorInformation { get; private set; }

        public ConnectionMode Mode
        {
            get { return _connectionMode; }
            set
            {
                if (_connectionMode != value)
                {
                    _connectionMode = value;
                    this.NotifyPropertyChanged();
                    ApplicationSettingsHelper.StoreSetting<ConnectionMode>("Mode", _connectionMode);
                }
            }
        }

        public string PortNumber 
        { 
            get {return _portNumber;}
            private set
            {
                if (_portNumber != value)
                {
                    _portNumber = value;
                    this.NotifyPropertyChanged();
                    ApplicationSettingsHelper.StoreSetting("PortNumber", PortNumber);
                }
            }
        }

        public string ServerNameIP
        {
            get { return _serverNameIP; }
            private set
            {
                if (_serverNameIP != value)
                {
                    _serverNameIP = value;
                    this.NotifyPropertyChanged();
                    ApplicationSettingsHelper.StoreSetting("ServerIP", ServerNameIP);
                }
            }
        }

        public ObservableCollection<PeerInformation> BTDevices 
        { 
            get {return _bTDevices;}
        }

        public ObservableCollection<NMEADataItem> NMEADataPoints
        {
            get {return _nmeaData;}
        }
        
        public bool Connected
        {
            get { return _connectedSate; }
            private set
            {
                if (_connectedSate != value)
                {
                    _connectedSate = value;
                    this.NotifyPropertyChanged();
                    this.NotifyPropertyChanged("NotConnected");

                    //Save connected state so know to connect or not to on start
                    ApplicationSettingsHelper.StoreSetting("Connected", _connectedSate);
                }
            }
        }
        
        public bool NotConnected
        {
            get { return !this.Connected; }
        }

        public bool Connecting
        {
            get { return _connecting; }
            private set
            {
                if (_connecting != value)
                {
                    _connecting = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool LoggingEnabled
        {
            get { return _loggingEnabled; }
            private set
            {
                if (_loggingEnabled != value)
                {
                    _loggingEnabled = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public bool RunUnderLock
        {
            get { return _runUnderLock; }
            private set
            {
                if (_runUnderLock != value)
                {
                    _runUnderLock = value;
                    ApplicationSettingsHelper.RunUnderLock(_runUnderLock);
                    ApplicationSettingsHelper.StoreSetting("RunUnderLock", _runUnderLock);
                    this.NotifyPropertyChanged();
                }
            }
        }

        public string DataString
        {
            get { return _dataString; }
            set 
            { 
                _dataString = value;
                this.NotifyPropertyChanged();
            }
        }
        
        #endregion

        #region Methods

        public async Task ConnectAsync()
        {
            if (this.Connected == false)
            {
                Connecting = true;
                Connected = true;
                try
                {
                    if (Mode == ConnectionMode.DEMO)
                    {
                        this.SendRecBackgroundDemoMode();
                    }
                    if (Mode == ConnectionMode.UDP)
                    {
                        Analytics.LogEvent("ConnectingOnUDP");
                        _readUDPSocket = new DatagramSocket();
                        _readUDPSocket.MessageReceived += recSocket_MessageReceived;
                        await _readUDPSocket.BindServiceNameAsync(PortNumber);
                        Analytics.LogEvent("ConnectingOnUDPPass");
                    }
                    else if (Mode == ConnectionMode.TCP)
                    {
                        Analytics.LogEvent("ConnectingOnTCP");
                        _readSocket = new StreamSocket();
                        HostName name = new HostName(ServerNameIP);
                        await _readSocket.ConnectAsync(name, PortNumber); //TODO Timeout this on shorter timeout. http://msdn.microsoft.com/en-us/library/windows/apps/xaml/jj710176.aspx
                        
                        this.CreateReadDataStream();
                        Analytics.LogEvent("ConnectingOnTCPPass");
                    }
                    else if (Mode == ConnectionMode.BlueTooth)
                    {
                        Analytics.LogEvent("ConnectingOnBT");
                        _readSocket = new StreamSocket();
                        await _readSocket.ConnectAsync(this.CurrentDevice.HostName, "1");
                        
                        this.CreateReadDataStream();
                        Analytics.LogEvent("ConnectingOnBTPass");
                    }
                    else
                    {
                        //ToDo throw ... 
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    SocketErrorStatus socketError = SocketError.GetStatus(ex.HResult);
                    SocketErrorInformation = socketError.ToString();
                    CloseConnection();
                    throw;
                }
                finally
                {
                    Connecting = false;
                }
            }
            else
            {
                this.CloseConnection();
                this.Connected = false;
            }
        }

        public async Task RefeshBlueToothDevicesAsync()
        {
            if (this.Mode != NMEAData.ConnectionMode.BlueTooth)
            {
                //Not in BT mode do nothing
                return;
            }

            this.BTDevices.Clear();

            // Configure PeerFinder to search for all paired devices.
            try
            {
                PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
                var pairedDevices = await PeerFinder.FindAllPeersAsync();

                if (pairedDevices.Count == 0)
                {
                    DataString = "No Devices found";
                }
                else
                {
                    foreach (PeerInformation device in pairedDevices)
                    {
                        BTDevices.Add(device);
                    }
                }
            }
            catch (Exception)
            {
                DataString = "Bluetooth is not available enable it in settings.";
            }
            finally
            {
                PeerFinder.AlternateIdentities.Remove("Bluetooth:Paired");
            }
        }

        public void Disconnect()
        {
            this.CloseConnection();
        }

        private void SendRecBackgroundDemoMode()
        {
            //Start Background Thread reading from a file.
            Thread reader = new Thread(() =>
            {
                try
                {
                    System.Windows.Resources.StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri("Assets/DemoData/nmeadatafile.txt", UriKind.Relative));
                    using (StreamReader dataReader = new StreamReader(streamInfo.Stream))
                    {
                        while (this.Connected == true)
                        {
                            while (dataReader.EndOfStream != true && this.Connected == true)
                            {
                                string data = dataReader.ReadLine();
                                if (data.Length > 0)
                                {
                                    
                                    //DataLogger.Instance.LogData(data);

                                    if (NMEADataItem.IsNMEAValid(data))
                                    {
                                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                                        {
                                            DataString = data;
                                            NMEADataItem.DecodeNMEA(data, ref _nmeaData);
                                        });
                                    }
                                    //Dont Send To Fast.
                                    System.Threading.Thread.Sleep(100);
                                }
                            }

                            if (dataReader.EndOfStream == true)
                            {
                                dataReader.BaseStream.Position = 0;
                                dataReader.DiscardBufferedData();
                            }
                        }

                    }
                }
                catch(SystemException)
                {
                }
            });

            reader.IsBackground = true;
            reader.Start();
        }

        private void recSocket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            //Call back used on UDP connections we are quasi working as a listning server in this mode. 
            DataReader dataReader = args.GetDataReader();
            string data = dataReader.ReadString(dataReader.UnconsumedBufferLength);

            //DataLogger.Instance.LogData(data);

            foreach (string item in data.Split(new char[] {'\r','\n'}, StringSplitOptions.RemoveEmptyEntries) )
            {
                if (NMEADataItem.IsNMEAValid(item))
                {
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        NMEADataItem.DecodeNMEA(item, ref _nmeaData);
                    });
                }
            }

            Debug.WriteLine(data);
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                DataString = data;
            });
        }
         
        private void CreateReadDataStream()
        {
            //This is the polling code when using TCP socket we read async x bytes then decode and update.
            _dataReader = new DataReader(this._readSocket.InputStream);
            _dataReader.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;

            _pollingThread = new Thread(() =>
            {
                try
                {
                    byte[] databytes = new byte[ReadBufferSize];

                    AutoResetEvent waitEvent = new AutoResetEvent(false);
                    IAsyncOperation<uint> loadAsyncOp = null;
                    AsyncOperationCompletedHandler<uint> handler = new AsyncOperationCompletedHandler<uint>((IAsyncOperation<uint> asyncinfo, AsyncStatus status) =>
                    {
                        waitEvent.Set();
                    });
                    
                    string prevPartial = string.Empty;
                    while (_dataReader != null)
                    {
                        loadAsyncOp = _dataReader.LoadAsync(ReadBufferSize);
                        loadAsyncOp.Completed += handler;
                        waitEvent.WaitOne();

                        var bytesread = loadAsyncOp.GetResults();
                        _dataReader.ReadBytes(databytes);
                        UTF8Encoding data = new UTF8Encoding();
                        string dataSentance  = data.GetString(databytes, 0, databytes.Length);
                        foreach (string item in dataSentance.Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string dataItem = item.Trim();
                            if (item[0] != '$' && !string.IsNullOrWhiteSpace(prevPartial))
                            {
                                dataItem = prevPartial + item;
                            }
                            else
                            {
                                dataItem = item;
                            }

                            if (NMEADataItem.IsNMEAValid(dataItem))
                            {
                                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                                {
                                    NMEADataItem.DecodeNMEA(dataItem, ref _nmeaData);
                                });
                            }
                            else
                            {
                                prevPartial = dataItem;
                            }
                        }

                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            DataString = prevPartial + dataSentance;
                        });
                    }
                }
                catch (ThreadAbortException)
                {
                    //Not an error exiting
                }
                catch (System.Exception)
                {
                    this.CloseConnection();
                }
            });

            _pollingThread.Start();
        }

        private void CloseConnection()
        {
            //Close the polling thread 
            if (_pollingThread != null)
            {
                _pollingThread.Abort();
                _pollingThread = null;
            }

            //Dispose of resources
            if (_dataReader != null)
            {
                _dataReader.Dispose();
                _dataReader = null;
            }

            if (_readSocket != null)
            {
                _readSocket.Dispose();
                _readSocket = null;
            }

            if (_readUDPSocket != null)
            {
                _readUDPSocket.Dispose();
                _readUDPSocket = null;
            }

            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                DataString = string.Empty;
            });

            Connected = false;
        }

        #endregion

        #region NotifyPropetyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called. 
            if(!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources. 
                if(disposing)
                {
                    // Dispose managed resources.
                    this.CloseConnection();
                }
                        
                //Clean up unmanaged data here. 

                //Done
                disposed = true;
            }
        }

        #endregion

    }
}
