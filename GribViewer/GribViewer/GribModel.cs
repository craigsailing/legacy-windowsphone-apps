using GribDecoder;
using Microsoft.Phone.Maps.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Device.Location;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GribViewer
{
    public class GRIBModel : INotifyPropertyChanged
    {
        public GRIBModel() 
        {
            //Load Default Grib Download Settings (These get changed by user of loaded Grib File)
            Resolution = 1;
            Days = 3;
            Interval = 12;
            WeatherLayers = WeatherLayers.WIND;
            CurrentForecastInterval = -1;
            IsDataLoaded = false;
        }

        public void SaveAppState()
        {
            //Save current app data that is spec to the session for resume from ThombStone;
            ApplicationSettingsHelper.StoreSetting<double>("MapZoom", this.MapZoom);
            ApplicationSettingsHelper.StoreSetting<GeoCoordinate>("MapCenter", this.Center);
            ApplicationSettingsHelper.StoreSetting<GeoCoordinate>("PositionLast", this.PosLastInfo);
            ApplicationSettingsHelper.StoreSetting<int>("CurrentForcastInterval", this.CurrentForecastInterval); 
        }

        public void LoadAppState()
        {
            //Load last interval map center zoom and geo level.
            double x;
            if (ApplicationSettingsHelper.TryGetSetting<double>("MapZoom", out x))
            {
                this.MapZoom = x;
            }

            GeoCoordinate center;
            if (ApplicationSettingsHelper.TryGetSetting<GeoCoordinate>("MapCenter", out center))
            {
                this.Center = center;
            }

            GeoCoordinate xy;
            if (ApplicationSettingsHelper.TryGetSetting<GeoCoordinate>("PositionLast", out xy))
            {
                this.PosLastInfo = xy;
            }

            int interval;
            if (ApplicationSettingsHelper.TryGetSetting<int>("CurrentForcastInterval", out interval))
            {
                this.CurrentForecastInterval = interval;
            }

        }

        private GribDecoder.GribDecoder _GRIB = null;

        //Map Properties
        public double MapZoom { get; set; }
        public GeoCoordinate Center { get; set; }
        public GeoCoordinate Pos0 { get; set; }
        public GeoCoordinate Pos1 { get; set; }
        public GeoCoordinate PosLastInfo { get; set; }
        public double MapDisplayResolution { get; set; }

        private int _crrentForcastInterval = 0;
        public int CurrentForecastInterval
        {
            get
            {
                return _crrentForcastInterval;
            }
            set
            {
                if (_crrentForcastInterval != value)
                {
                    _crrentForcastInterval = value;  
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("ForcastPeriod");
                    NotifyPropertyChanged("ForcastPeriodDetailed");
                }
            }
        }
     
        //Grib Properties
        public int Days { get; set; }
        public int Interval  { get; set; }
        public double Resolution { get; set; }
        public int IntervalMax { get; set; }
        
        public bool WindFC
        {
            get
            {
                return (this.WeatherLayers & WeatherLayers.WIND) == WeatherLayers.WIND ? true : false;
            }
            set
            {
                if (value)
                    this.WeatherLayers |= WeatherLayers.WIND;
                else
                    this.WeatherLayers &= ~WeatherLayers.WIND;
            }
        }

        public bool PressureFC
        {
            get
            {
                return (this.WeatherLayers & WeatherLayers.PRESS) == WeatherLayers.PRESS ? true : false;
            }
            set
            {
                if (value)
                    this.WeatherLayers |=  WeatherLayers.PRESS;
                else
                    this.WeatherLayers &=  ~WeatherLayers.PRESS;
            }
        }
       
        public GeoCoordinate GribCenter 
        { 
            get
            {
                if (Grid.Count > 0)
                {
                    double LatCenter = 0;
                    double LonCenter = 0;
                    if (Grid[0].Latitude > Grid[Grid.Count - 1].Latitude)
                    {
                        LatCenter = Grid[Grid.Count - 1].Latitude + ((Grid[0].Latitude - Grid[Grid.Count - 1].Latitude) / 2);
                    }
                    else
                    {
                        LatCenter = Grid[0].Latitude + ((Grid[Grid.Count - 1].Latitude - Grid[0].Latitude) / 2);
                    }

                    if (Grid[0].Longitude > Grid[Grid.Count - 1].Longitude)
                    {
                        LonCenter = Grid[Grid.Count - 1].Longitude + ((Grid[0].Longitude - Grid[Grid.Count - 1].Longitude) / 2);
                    }
                    else
                    {
                        LonCenter = Grid[0].Longitude + ((Grid[Grid.Count - 1].Longitude - Grid[0].Longitude) / 2);
                    }

                    return new GeoCoordinate(LatCenter, LonCenter);
                }
                else
                {
                    return new GeoCoordinate(0, 0);
                }
            } 
        }

        public double GridWidth
        {
            get
            {
                if (Grid.Count > 0)
                {
                    double LonWidth = 0;
                    if (Grid[0].Longitude > Grid[Grid.Count - 1].Longitude)
                    {
                        LonWidth = (Grid[0].Longitude - Grid[Grid.Count - 1].Longitude) / 2;
                    }
                    else
                    {
                        LonWidth = (Grid[Grid.Count - 1].Longitude - Grid[0].Longitude) / 2;
                    }
                    return LonWidth;
                }
                else
                {
                    return 0; ;
                }
            }
        }

        public double GridHight
        {
            get
            {
                if (Grid.Count > 0)
                {
                    double LatWidth = 0;
                    if (Grid[0].Latitude > Grid[Grid.Count - 1].Latitude)
                    {
                        LatWidth = (Grid[0].Latitude - Grid[Grid.Count - 1].Latitude) / 2.0;
                    }
                    else
                    {
                        LatWidth =(Grid[Grid.Count - 1].Latitude - Grid[0].Latitude) / 2.0;
                    }

                    return LatWidth;
                }
                else
                {
                    return 0; ;
                }
            }
        }
        
        public DateTime ForcastPeriod
        {
            get
            {
                if (_GRIB != null && _GRIB.ForeCastTime != null)
                {
                    return _GRIB.ForeCastTime + new TimeSpan(CurrentForecastInterval * Interval, 0, 0);
                }
                else
                {
                    return DateTime.Now;
                }
            }
        }

        public string ForcastPeriodDetailed
        {
            get
            {
                 return string.Format("FC: {0} {1}", ForcastPeriod, "UTC");
            }
        }

        //Forcast Properties
        public WeatherLayers WeatherLayers { get; set; }

        public string ForeCastTime 
        {
            get
            {
                if (_GRIB != null && _GRIB.ForeCastTime != null)
                {
                    return _GRIB.ForeCastTime.ToString();
                }
                {
                    return string.Empty;
                }
            }
        }

        public DateTime ForCastDateTime
        {
            get
            {
                if (_GRIB != null && _GRIB.ForeCastTime != null)
                {
                    return _GRIB.ForeCastTime;
                }
                {
                    return DateTime.Now;
                }
            }
        }
        
        //General  
        public bool IsDataLoaded  { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public async Task<bool> LoadData(bool loadSavedAppState = false)
        {
            //Check if the model has copied the data to iso if not wait for it 

            //Load Grib Data
            this._GRIB = new GribDecoder.GribDecoder();
            await _GRIB.Decode("current.grb");
            
            //Loaded new grib set details of the grib
            CurrentForecastInterval = 0;
            Resolution = MapDisplayResolution = _GRIB.ResolutionLat;
            Days = _GRIB.ForeCastEndHours / 24; ;
            Interval = _GRIB.ForCastIntervaLCount > 1 ? _GRIB.ForeCastEndHours / (_GRIB.ForCastIntervaLCount - 1): _GRIB.ForeCastEndHours;
            IntervalMax = _GRIB.ForCastIntervaLCount;

            if (loadSavedAppState == true)
                LoadAppState();

            IsDataLoaded = true;
            return IsDataLoaded;
        }
                
        public string EmailQueryString()
        {
            string model = "gfs";
           
            //SailDocs
            //Model:Lat0,Lat1,Lon0,Lon1|res,res|range|param : Uses W/E/N/S (Caps)
            //Neg is south and west, whole INT on lat lon.
            //Res 0.5, 1, 2
            //Range start,int..end
            
            double latn0 = Pos0.Latitude;
            double latn1 = Pos1.Latitude;

            //Cap the lat at 80 deg N and S
            if (latn0 > 80)
                latn0 = 80;
            
            if (latn0 < -80)
                latn0 = -80;
            
            if (latn1 > 80)
                latn1 = 80;
  
            if (latn1 < -80)
                latn1 = -80;

            string lat0 = string.Format(CultureInfo.InvariantCulture, "{0:0}{1}", Math.Abs(latn0), (Pos0.Latitude < 0 ? "S" : "N"));
            string lon0 = string.Format(CultureInfo.InvariantCulture, "{0:0}{1}", Math.Abs(Pos0.Longitude), (Pos0.Longitude < 0 ? "W" : "E"));
            string lat1 = string.Format(CultureInfo.InvariantCulture, "{0:0}{1}", Math.Abs(latn1), (Pos1.Latitude < 0 ? "S" : "N"));
            string lon1 = string.Format(CultureInfo.InvariantCulture, "{0:0}{1}", Math.Abs(Pos1.Longitude), (Pos1.Longitude < 0 ? "W" : "E"));
                       
            //? Is the format there spec for decimal place.
            string resLat = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", Resolution);
            string resLon = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", Resolution);
                                 
            string vts =  string.Format ("{0},{1}..{2}", 0, Interval, Days * 24);

            string layers = string.Empty;
            if ( (this.WeatherLayers & WeatherLayers.WIND) == WeatherLayers.WIND)
            {
                layers = "WIND,";
            }
            if ((this.WeatherLayers & WeatherLayers.PRESS) == WeatherLayers.PRESS)
            {
                layers = layers + "PRESS,";
            }
            layers = layers.Trim(',');

            string data = string.Format(CultureInfo.InvariantCulture, 
                                        "{0}:{1},{2},{3},{4}|{5},{6}|{7}|{8}\r\n\r\n", 
                                        model, lat1, lat0, lon0, lon1, resLat, resLon, vts, layers);

            return data;
        }

        //Access to the Weather Data
        public List<GeoCoordinate> Grid { get { return _GRIB.Grid(Paramater.UGRD); } }

        public void Wind(int forcastItteration, List<int> angleList, List<double> speedList)
        {
            _GRIB.DecodeWind(forcastItteration, angleList, speedList);
        }

        public void Wind(GeoCoordinate pos, List<int> angleList, List<double> speedList)
        {
            _GRIB.DecodeWind(pos, angleList, speedList);
        }

        public void Pressure(int forcastItteration, List<double> pressureList)
        {
            _GRIB.DecodePressure(forcastItteration, pressureList);
        }

        public void Pressure(GeoCoordinate pos, List<double> pressureList)
        {
            _GRIB.DecodePressure(pos, pressureList);
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
        #endregion
    }

    [Flags]
    public enum WeatherLayers
    {
        NONE = 0,
        WIND = 1,
        PRESS = 2,
        APCP = 4,
        AIRTEMP = 8,
        SEATEMP = 16,
        WAVES = 32
    }
}
