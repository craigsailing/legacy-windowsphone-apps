using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NMEAShared
{
    public class NMEADataItem : INotifyPropertyChanged
    {

        private static string ConvertLatLonToString(string data)
        {
            try
            {
                //If this is a GPS NMEA sentance $IIGLL or $GPGLL
                string LongNS;
                string LatEW;
                string[] tokendata = data.Split(',');
                string lat = tokendata[1];
                string latNS = tokendata[2];
                string lon = tokendata[3];
                string lonWE = tokendata[4];

                if (string.IsNullOrEmpty(lat) || string.IsNullOrEmpty(lon))
                    return string.Empty;

                string[] temp = lat.Split('.');
                string LatDeg = temp[0].Substring(0, temp[0].Length - 2);
                string LatMin = temp[0].Substring(temp[0].Length - 2) + "." + temp[1];

                temp = lon.Split('.');
                string LongDeg = temp[0].Substring(0, temp[0].Length - 2);
                string LongMin = temp[0].Substring(temp[0].Length - 2) + "." + temp[1];

                if (tokendata[2].ToUpper().Contains('S'))
                    LongNS = "S";
                else
                    LongNS = "N";

                if (tokendata[4].ToUpper().Contains('W'))
                    LatEW = "W";
                else
                    LatEW = "E";


                string outputLat = string.Format("{0}{1} {2}{3} {4}\n", LatDeg, "°", LatMin, "' ", LongNS);
                string outputLon = string.Format("{0}{1} {2}{3} {4}", LongDeg, "°", LongMin, "'", LatEW);


                return outputLat + outputLon;
            }
            catch (System.Exception)
            {
                //Catch all never fail return empty data
                return string.Empty;
            }
        }

        private static void ConvertLatLonToDouble(string data, ref double dlat, ref double dlon)
        {
            try
            {
                //If this is a GPS NMEA sentance $IIGLL or $GPGLL
                string[] tokendata = data.Split(',');
                string lat = tokendata[1];
                string latNS = tokendata[2];
                string lon = tokendata[3];
                string lonWE = tokendata[4];

                if (string.IsNullOrEmpty(lat) || string.IsNullOrEmpty(lon))
                {
                    //TODO look at this invalid gps data...
                    dlat = 0;
                    dlon = 0;
                    return;
                }

                //Convert to KML format decimal degrees.
                string[] temp = lat.Split('.');
                string LatDeg = temp[0].Substring(0, temp[0].Length - 2);
                string LatMin = temp[0].Substring(temp[0].Length - 2) + "." + temp[1];
                temp = lon.Split('.');
                string LongDeg = temp[0].Substring(0, temp[0].Length - 2);
                string LongMin = temp[0].Substring(temp[0].Length - 2) + "." + temp[1];

                dlat = Convert.ToDouble(LatDeg) + Convert.ToDouble(LatMin) / 60;
                dlon = Convert.ToDouble(LongDeg) + Convert.ToDouble(LongMin) / 60;

                if (tokendata[2].ToUpper().Contains('S'))
                {
                    //South is neg deg in kml
                    dlat = dlat * (-1);
                }

                if (tokendata[4].ToUpper().Contains('W'))
                {
                    //West is neg deg in kml
                    dlon = dlon * (-1);
                }
            }
            catch (System.Exception) { dlat = 0; dlon = 0; }
        }

        private static void UpdateData(ref ObservableCollection<NMEADataItem> nMEAItemCollection, TAG tag, double data, string dataSecondary = "")
        {
            foreach (NMEADataItem NMEAItem in nMEAItemCollection)
            {
                if (NMEAItem.Tag == tag)
                {
                    NMEAItem.Data = data;
                    NMEAItem.DataSecondary = dataSecondary;
                }
            }
        }

        public static bool IsNMEAValid(string data)
        {

            //Checks the Check sum on the data
            int checksum = 0;
            if (data.Length <= 6 || data[0] != '$')
                return false;

            foreach (char c in data)
            {
                if (c == '$')
                    continue;
                if (c == '*')
                    break;

                if (checksum == 0)
                    checksum = Convert.ToByte(c);
                else
                {
                    try
                    {
                        checksum = checksum ^ Convert.ToByte(c);
                    }
                    catch (System.OverflowException)
                    {
                        checksum = 0;
                    }
                }
            }

            if (data.Substring(data.IndexOf('*') + 1) == checksum.ToString("X2"))
                return true;
            else
                return false;
        }

        public static void DecodeNMEA(string data, ref ObservableCollection<NMEADataItem> nMEAItemCollection)
        {
            //if (NMEADataItem.IsNMEAValid(data) == false)
            // return;

            double value = 0;
            try
            {
                string type = data.Substring(3, 3).ToUpper();
                string[] temp = data.Split(',');
                if (type == "VHW")
                {
                    //Water Speed and Heading Magnetic
                    if (double.TryParse(temp[5], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.VHW, value);

                    if (double.TryParse(temp[3], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.HMAG, value);

                    return;
                }

                if (type == "VTG")
                {
                    //track and speed over ground
                    if (double.TryParse(temp[1], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.COG, value);

                    if (double.TryParse(temp[5], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.SOG, value);

                    return;
                }

                if (type == "VPW")
                {
                    //Speed to the windard
                    if (double.TryParse(temp[1], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.VPW, value);

                    return;
                }

                if (type == "VWR")
                {
                    //Relative Wind Info
                    if (double.TryParse(temp[1], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.AWA, value, (temp[2] == "R") ? "Starboard" : "Port");

                    if (double.TryParse(temp[3], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.AWS, value);

                    return;
                }

                if (type == "VWT")
                {
                    //True Wind 
                    if (double.TryParse(temp[1], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.TWA, value);

                    if (double.TryParse(temp[3], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.TWS, value);

                    return;
                }

                if (type == "MWD")
                {
                    //True Wind Dir Magnetic
                    if (double.TryParse(temp[3], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.TWD, value);

                    return;
                }

                if (type == "RSA")
                {
                    //Rudder Angle
                    //if (double.TryParse(temp[1], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    //NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.RSA, value);
                    return;
                }

                if (type == "GLL")
                {
                    //GPS Info Lat and Long
                    string LatLong = ConvertLatLonToString(data);
                    NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.POS, 0, LatLong);
                    return;
                }

                if (type == "RMC")
                {
                    //GPS Info Lat and Long
                    //double dlat = 0, dlon = 0;
                    //ConvertLatLonToDouble(data, ref dlat, ref dlon);
                    //nmeaData.Lat = dlat;
                    //nmeaData.Lon = dlon;
                    //nmeaData.Time = temp[5];

                    //nmeaData.TrackMag = Convert.ToDouble(temp[8]);
                    //nmeaData.SpeedOverGround = Convert.ToDouble(temp[7]);

                    return;
                }

                if (type == "RMB")
                {
                    if (double.TryParse(temp[10], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.DTW, value);

                    if (double.TryParse(temp[11], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.BTW, value);

                    if (!string.IsNullOrEmpty(temp[5]))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.WPT, 0, temp[5]);

                    if (double.TryParse(temp[12], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.VMG, value);
                    else
                    {
                        //If it is not present then calculate it.
                        //TODO
                    }
                    return;
                }

                if (type == "BWC")
                {
                    /*
                    if (double.TryParse(temp[10], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.DTW, value);

                    if (double.TryParse(temp[6], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.BTW, value);

                    if (!string.IsNullOrEmpty(temp[12]))
                        NMEADataItem.UpdateData(ref nMEAItemCollection, NMEADataItem.TAG.WPT, 0, temp[4]);
                    */
                    return;
                }
            }
            catch (System.FormatException)
            {
                //Discard any data cant parse out.
            }
            return;
        }

        public static void BuildDataCollection(ref ObservableCollection<NMEADataItem> data)
        {
            if (data.Count > 0)
                data.Clear();

            foreach (TAG tag in Enum.GetValues(typeof(TAG)))
            {
                if (tag != TAG.UKNOWN)
                    data.Add(new NMEADataItem(tag));
            }
        }

        #region .ctor
        public NMEADataItem()
        {
            this.Init();
        }

        public NMEADataItem(TAG tag)
            : this(0, tag, string.Empty)
        {
        }

        public NMEADataItem(double data, NMEADataItem.TAG tag)
            : this(data, tag, string.Empty)
        {
        }

        public NMEADataItem(double data, NMEADataItem.TAG tag, string dataSecondary)
        {
            Init();
            this.Data = data;
            this.Tag = tag;
            this.DataSecondary = dataSecondary;
            this.MapGroup();
            this.MapUnits();

            //TODO Map these to full strings and localize NB.
            this.DisplayName = Tag.ToString();
        }

        private void Init()
        {
            Data = 0;
            Max = 0;
            Min = double.MaxValue;
            DataSecondary = string.Empty;
            DisplayName = string.Empty;
        }

        private void MapUnits()
        {
            switch (this._tag)
            {
                case TAG.VHW:
                case TAG.SOG:
                case TAG.VPW:
                case TAG.TWS:
                case TAG.AWS:
                case TAG.VMG:
                    this.Units = "Kt";
                    break;

                case TAG.TWD:
                case TAG.TWA:
                case TAG.AWA:
                case TAG.BTW:
                case TAG.COG:
                case TAG.HMAG:
                    this.Units = "Deg";
                    break;

                case TAG.DTW:
                    this.Units = "NM";
                    break;

                default:
                    break;
            }
        }

        private void MapGroup()
        {
            //Primary Mappings
            switch (this._tag)
            {
                case TAG.VHW:
                case TAG.SOG:
                case TAG.COG:
                case TAG.VPW:
                    this.Group = this.Group | GROUP.Speed;
                    break;

                case TAG.TWD:
                case TAG.TWS:
                case TAG.TWA:
                case TAG.AWA:
                case TAG.AWS:
                    this.Group = this.Group | GROUP.Wind;
                    break;

                case TAG.VMG:
                case TAG.BTW:
                case TAG.DTW:
                case TAG.WPT:
                case TAG.POS:
                    this.Group = this.Group | GROUP.Navigation;
                    break;

                default:
                    break;
            }

            //Secondary Mappings
            switch (this._tag)
            {
                case TAG.TWA:
                case TAG.COG:
                case TAG.VHW:
                case TAG.HMAG:
                    this.Group = this.Group | GROUP.Helm;
                    break;

                default:
                    break;
            }

            //Terciary Mappings if Needed

        }
        #endregion

        public enum TAG
        {
            UKNOWN,
            VHW,
            SOG,
            COG,
            HMAG,
            AWS,
            AWA,
            TWS,
            TWA,
            TWD,
            VPW,
            VMG,
            BTW,
            DTW,
            WPT,
            POS,
            AIS,
        }

        [FlagsAttribute]
        public enum GROUP : uint
        {
            None = 0,
            Helm = 1,
            Speed = 2,
            Wind = 4,
            Navigation = 8,
            AIS = 16,
            Favourites = 32
        }

        #region members
        private GROUP _group;
        private TAG _tag;
        private double _data;
        private double _dataMin;
        private double _dataMax;
        private string _dataScondary;
        private string _displayName;
        private string _units;
        #endregion

        #region Properties
        public GROUP Group
        {
            get { return this._group; }
            private set
            {
                if (this._group != value)
                {
                    this._group = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public TAG Tag
        {
            get { return this._tag; }
            private set
            {
                if (this._tag != value)
                {
                    this._tag = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public double Data
        {
            get { return this._data; }
            private set
            {
                if (this._data != value)
                {
                    this._data = value;
                    if (this._data < this.Min)
                    {
                        this.Min = this._data;
                    }
                    if (this._data > this.Max)
                    {
                        this.Max = this._data;
                    }

                    this.NotifyPropertyChanged();
                }
            }
        }

        public double Min
        {
            get { return this._dataMin; }
            private set
            {
                if (this._dataMin != value)
                {
                    this._dataMin = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public double Max
        {
            get { return this._dataMax; }
            private set
            {
                if (this._dataMax != value)
                {
                    this._dataMax = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public string DataSecondary
        {
            get { return this._dataScondary; }
            private set
            {
                if (this._dataScondary != value)
                {
                    this._dataScondary = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public string DisplayName
        {
            get { return this._displayName; }
            private set
            {
                if (this._displayName != value)
                {
                    this._displayName = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public string Units
        {
            get { return this._units; }
            private set
            {
                if (this._units != value)
                {
                    this._units = value;
                    this.NotifyPropertyChanged();
                }
            }
        }
        #endregion

        #region NotifyPropetyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
