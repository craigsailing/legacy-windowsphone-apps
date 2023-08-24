using Microsoft.Phone.Maps.Controls;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GribViewer
{
    class PressureItem
    {
        public double Pressure { get; set; }
        public GeoCoordinate Position { get; set; }

        private MapOverlay _overLay = null;
        public MapOverlay Overlay
        {
            get
            {
                if (_overLay == null)
                {
                    //_overLay = BuildWindBarb(1, this.Angle, this.Speed, this.Position);
                }
                return _overLay;
            }
        }

        public PressureItem(double pressure, GeoCoordinate position)
        {
            //Brushes();                  //Brushes are static 1 shared created when first used and then preserved.
            Pressure = pressure;
            Position = position;        //Delay creating the overlay created on first prop usage or access.
        }
     }

    class PressureLayer
    {
        //Make this a singleton. Should only have  instance of a specific layer
        static private PressureLayer _instance = null;
        private List<PressureItem> _pressureList = null;
        private List<PressureItem> _pressureListFiltered = null;
        private int _interval = -1;
        private int _filterLevel = 0;

        private PressureLayer()
        {
            _pressureList = new List<PressureItem>();
            _pressureListFiltered = new List<PressureItem>();
        }

        static public PressureLayer Instance()
        {
            if (_instance == null)
            {
                _instance = new PressureLayer();
            }

            //Load new data if needed;
            if (_instance._interval != App.Model.CurrentForecastInterval)
            {
                _instance._interval = App.Model.CurrentForecastInterval;
                _instance.LoadRawData();
            }

            return _instance;
        }

        private void LoadRawData()
        {
            List<double> pressure = new List<double>();
            App.Model.Pressure(App.Model.CurrentForecastInterval, pressure);
            this._pressureList.Clear();

            if (pressure.Count > 0)
                App.Model.WeatherLayers |= WeatherLayers.PRESS;

            for (int i = 0; i < pressure.Count; i++)
            {
                //TODO problem here need to use the grid for this item BUGBUG refactore this for wind also
                _pressureList.Add(new PressureItem(pressure[i], App.Model.Grid[i]));
            }
        }

        public List<MapOverlay> LayerView(double baseLevel = 0)
        {
            FilterOverlays(baseLevel);

            List<MapOverlay> tmp = new List<MapOverlay>(from pressure in this._pressureListFiltered select pressure.Overlay);
            return tmp;
        }

        public bool DataAtPoint(GeoCoordinate xy, ref double pressure)
        {
            //round the data point
            GeoCoordinate point = new GeoCoordinate(Math.Round(xy.Latitude, 1), Math.Round(xy.Longitude, 1));

            //Get a smaller Geo Region if not on the base layer
            List<PressureItem> points = new List<PressureItem>();
            double res = 0.5;
            while (points.Count == 0 && res < 4)
            {
                points = new List<PressureItem>(from pressureItem in _pressureList
                                                where pressureItem.Position.Latitude < point.Latitude + res &&
                                                pressureItem.Position.Latitude > point.Latitude - res &&
                                                pressureItem.Position.Longitude > point.Longitude - res &&
                                                pressureItem.Position.Longitude < point.Longitude + res
                                                select pressureItem);
                res = res * 2;
            }

            //Get closest point of interest now.
            double dist = 0;
            double distSmallest = Double.MaxValue;
            PressureItem result = null;
            foreach (PressureItem item in points)
            {
                dist = item.Position.GetDistanceTo(point);
                if (dist < distSmallest)
                {
                    distSmallest = dist;
                    result = item;
                }
            }

            //There is data at this location
            if (result != null)
            {
                pressure = result.Pressure;
                return true;
            }

            //Did not find data
            return false;
        }

        private void FilterOverlays(double baseLevel)
        {
            const int elementCount = 350;

            //System.Diagnostics.Debug.WriteLine("+FilterOverlays");
            //If baselevel is 0 then low all the grib filtered down this is the initial load.
            List<PressureItem> tmp = _pressureList;
            if (baseLevel != 0)
            {
                //Get a smaller Geo Region if not on the base layer
                int offset = 3;
                tmp = new List<PressureItem>(from pressureItem in _pressureList
                                             where pressureItem.Position.Latitude < App.Model.Pos0.Latitude + offset &&
                                                pressureItem.Position.Latitude > App.Model.Pos1.Latitude - offset &&
                                                pressureItem.Position.Longitude > App.Model.Pos0.Longitude - offset &&
                                                pressureItem.Position.Longitude < App.Model.Pos1.Longitude + offset
                                             select pressureItem);
            }

            //Zoom Level is the master and the number of data points in the overlay < less than 400 return
            if (tmp.Count <= elementCount)
            {
                _filterLevel = 0;
                _pressureListFiltered = tmp;
            }
            else
            {

                if (tmp.Count / 4 < elementCount)
                {
                    // 1/2 res
                    _filterLevel = 2;
                }
                else if (tmp.Count / 16 < elementCount)
                {
                    // 1/4 res
                    _filterLevel = 4;
                }
                else
                {
                    // 1/8 res
                    _filterLevel = 8;
                }

                this._pressureListFiltered.Clear();
                int latCounter = 0;
                int lonCounter = 0;

                for (int i = 0; i < tmp.Count; i++)
                {
                    if ((latCounter % _filterLevel == 0) &&
                         (lonCounter % _filterLevel == 0))
                    {
                        _pressureListFiltered.Add(tmp[i]);
                    }
                    lonCounter++;

                    if ((i + 1 < tmp.Count) &&
                        (tmp[i].Position.Latitude != tmp[i + 1].Position.Latitude))
                    {
                        latCounter++;
                        lonCounter = 0;
                    }
                }
            }
        }

    }

}
