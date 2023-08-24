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
    class WindItem
    {
        public int Angle { get; set; }
        public double Speed { get;  set; }
        public GeoCoordinate Position { get;  set;}

        private MapOverlay _overLay = null;
        public MapOverlay Overlay 
        {
            get
            {
                if (_overLay == null)
                {
                    _overLay =  BuildWindBarb(1, this.Angle, this.Speed, this.Position);
                }
                return _overLay;
            }
        }

        private static List<SolidColorBrush> _brushes = null;

        private void Brushes() 
        {
            if (_brushes == null)
            {
                _brushes = new List<SolidColorBrush>();
                _brushes.Add(new SolidColorBrush(Colors.Gray));
                _brushes.Add(new SolidColorBrush(Colors.Cyan));
                _brushes.Add(new SolidColorBrush(Colors.Blue));
                _brushes.Add(new SolidColorBrush(Colors.Green));
                _brushes.Add(new SolidColorBrush(Colors.Yellow));
                _brushes.Add(new SolidColorBrush(Colors.Orange));
                _brushes.Add(new SolidColorBrush(Colors.Red));
            }
        }

        public WindItem(int angle, double speed, GeoCoordinate position)
        {
            Brushes();                  //Brushes are static 1 shared created when first used and then preserved.
            Angle = angle;
            Speed = speed;
            Position = position;

            //Delay creating the overlay created on first prop usage or access.
       }

        private MapOverlay BuildWindBarb(double masterScale, int angle, double speed, GeoCoordinate position)
        {
            Polyline polyline = new Polyline();
            polyline.IsHitTestVisible = false;
            //polyline.CacheMode = new BitmapCache();
            
            //Create Base Barb
            const int strokeThickness = 2; 
            const int featherLength = 8; 
            const int barbLength = 32;  
            const int featherOffset = 4; 
            int featherLen = featherLength;

            bool teath = false;
            int offset = 0;
            polyline.Points.Add(new Point(0, 0));                //Start Barb at 0,0 eases the rotation offset problem

            //TODO handle 50Knts Barb
            // |
            // |
            // |
            // |--
            // |---

            double speedOrg = speed;
            while (speed > 0)
            {
                if (speed >= 7.5 && speed < 50)                 //10 Knot feathers
                {
                    speed -= 10;
                    teath = false;
                }
                else if (speed >= 2.5 && speed < 7.5)           //5 Knot feathers
                {
                    featherLen = (featherLength / 2) - strokeThickness;
                    speed -= 5;
                    teath = false;
                }
                else if (speed < 2.5)                           //dont draw 
                {
                    speed = 0;
                    teath = false;
                    break;
                }
                else if (speed >= 50)                           //50 Knot barbs
                {
                    speed -= 50;
                    teath = true;
                }

                if (!teath)
                {
                    //Common Case
                    polyline.Points.Add(new Point(0, barbLength - offset));                                     //Down Shaft
                    polyline.Points.Add(new Point(featherLen, barbLength - offset));                            //Feather to Right
                    polyline.Points.Add(new Point(0, barbLength - offset));                                     //Feather Back to shaft
                    offset += featherOffset;
                }
                else
                {
                    // >50 knt Case
                    polyline.Points.Add(new Point(0, barbLength - offset));                                     //Down Shaft
                    polyline.Points.Add(new Point(featherLength * 2, barbLength - offset - featherOffset));     //Barb to Right
                    polyline.Points.Add(new Point(0, barbLength - offset - (featherOffset * 2)));               //Barb Back to shaft
                    offset += featherOffset * 2;
                }
            }

            //Set the color and style
            polyline.Stroke = _brushes[6];
            if (speedOrg < 5)
            {
                masterScale = masterScale * 0.50;
                polyline.Stroke = _brushes[1];
            }
            if (speedOrg < 10 && speedOrg >= 5)
            {
                masterScale = masterScale * 0.60;
                polyline.Stroke = _brushes[2];
            }
            if (speedOrg < 15 && speedOrg >= 10)
            {
                masterScale = masterScale * 0.70;
                polyline.Stroke = _brushes[2];
            }
            if (speedOrg < 20 && speedOrg >= 15)
            {
                masterScale = masterScale * 0.80;
                polyline.Stroke = _brushes[3];
            }
            if (speedOrg < 30 && speedOrg >= 20)
            {
                masterScale = masterScale * 0.90;
                polyline.Stroke = _brushes[4];
            }
            if (speedOrg < 40 && speedOrg >= 30)
            {
                masterScale = masterScale * 1.00;
                polyline.Stroke = _brushes[5];
            }
            if (speedOrg >= 40)
            {
                masterScale = masterScale * 1.10;
                polyline.Stroke = _brushes[6];
            }

            polyline.StrokeThickness = strokeThickness;

            //Scale and Rotate the Barb :!TODO optamize and remove uneeded transforms
            RotateTransform rotateTransform = new RotateTransform();
            rotateTransform.Angle = angle - 180;    //Drew barb down so has to rotate 180 relative to wind angle
            rotateTransform.CenterX = 0;
            rotateTransform.CenterY = 0;

            //Flip the barb on the 0 axis and Master Scale
            ScaleTransform scaleTransform = new ScaleTransform();
            scaleTransform.ScaleX = -masterScale;
            scaleTransform.ScaleY = masterScale;

            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(rotateTransform);

            polyline.RenderTransformOrigin = new Point(0, 0);
            polyline.RenderTransform = transformGroup;

            MapOverlay overlay = new MapOverlay();
            overlay.Content = polyline;
            overlay.PositionOrigin = new Point(0, 0);
            overlay.GeoCoordinate = position;

            return overlay;
        }
    }

    class WindLayer
    {
        //Make this a singleton. Should only have  instance of a specific layer
        static private WindLayer _instance = null;
        private List<WindItem> _windList = null;
        private List<WindItem> _windListFiltered = null;
        private int _interval = -1;
        private int _filterLevel = 0;

        private WindLayer()
        {
            _windList = new List<WindItem>();
            _windListFiltered = new List<WindItem>();
            //LoadRawData();
        }

        static public WindLayer Instance()
        {
            if (_instance == null)
            {
                _instance = new WindLayer();
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
            List<double> speed = new List<double>();
            List<int> angle = new List<int>();
            App.Model.Wind(App.Model.CurrentForecastInterval, angle, speed);
            
            _windList.Clear();

            for (int i = 0; i < speed.Count; i++)
            {
                _windList.Add(new WindItem(angle[i], speed[i], App.Model.Grid[i]));
            }
        }

        public List<MapOverlay> LayerView(double baseLevel = 0)
        {
            System.Diagnostics.Debug.WriteLine("+LayerView");

            FilterOverlays(baseLevel);

            List<MapOverlay> tmp = new List<MapOverlay>(from wind in this._windListFiltered select wind.Overlay);

            System.Diagnostics.Debug.WriteLine("-LayerView");
            return tmp;
        }

        public bool DataAtPoint(ref GeoCoordinate xy, ref double windSpeed, ref int windAngle)
        {
            //round the data point
            GeoCoordinate point = new GeoCoordinate(Math.Round(xy.Latitude, 1), Math.Round(xy.Longitude, 1));
            
            //Get a smaller Geo Region if not on the base layer
            List<WindItem> points = new List<WindItem>();
            double res = 0.5;
            while (points.Count == 0 && res < 4)
            {
                points = new List<WindItem>( from windItem in _windList
                                             where windItem.Position.Latitude < point.Latitude + res &&
                                             windItem.Position.Latitude > point.Latitude - res &&
                                             windItem.Position.Longitude > point.Longitude - res &&
                                             windItem.Position.Longitude < point.Longitude + res
                                             select windItem);
                res = res * 2;
            }

            //Get closest point of interest now.
            double dist = 0;
            double distSmallest = Double.MaxValue;
            WindItem result = null;
            foreach(WindItem item in points)
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
                xy = result.Position;
                windAngle = result.Angle;
                windSpeed = result.Speed;
                return true;
            }

            //Did not find data
            return false;
        }

        private void FilterOverlays(double baseLevel)
        {
            const int elementCount = 350;

            //Get a smaller Geo Region if not on the base layer
            //System.Diagnostics.Debug.WriteLine("+FilterOverlays");
            //If baselevel is 0 then low all the grib filtered down this is the initial load.
            List<WindItem> tmp = _windList;
            if (baseLevel != 0)
            {
                double delta = 0;
                if (App.Model.Pos0.Latitude < App.Model.Pos1.Latitude)
                {
                    delta = Math.Abs(App.Model.Pos1.Latitude - App.Model.Pos0.Latitude) * 0.1;
                }
                else
                {
                    delta = Math.Abs(App.Model.Pos0.Latitude - App.Model.Pos1.Latitude) * 0.1;
                }
                if (delta > 4) delta = 4;

                //Special case the 180 lon W and E of 180 +179(E Hemp) -179 (West Hemp)
                double lon0 = GribDecoder.GribHelpers.WrapLongitude(App.Model.Pos0.Longitude - delta);
                double lon1 = GribDecoder.GribHelpers.WrapLongitude(App.Model.Pos1.Longitude + delta);
                if (lon0 > 0 && lon1 < 0)
                {
                    tmp = new List<WindItem>(from windItem in _windList
                                             where windItem.Position.Latitude < App.Model.Pos0.Latitude + delta &&
                                                windItem.Position.Latitude > App.Model.Pos1.Latitude - delta &&
                                               
                                                ( (windItem.Position.Longitude >= -180 &&
                                                windItem.Position.Longitude <= lon1) ||
                                                (windItem.Position.Longitude >= lon0 &&
                                                 windItem.Position.Longitude <= 180) )

                                             select windItem);

                }
                else
                {
                    //Get a smaller Geo Region if not on the base layer
                    tmp = new List<WindItem>(from windItem in _windList
                                             where windItem.Position.Latitude < App.Model.Pos0.Latitude + delta &&
                                                windItem.Position.Latitude > App.Model.Pos1.Latitude - delta &&
                                                windItem.Position.Longitude > App.Model.Pos0.Longitude - delta &&
                                                windItem.Position.Longitude < App.Model.Pos1.Longitude + delta
                                             select windItem);
                }
            }
          
            //Zoom Level is the master and the number of data points in the overlay < less than 400 return
            if (tmp.Count <= elementCount)
            {
                _filterLevel = 0;
                _windListFiltered = tmp;
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

                _windListFiltered.Clear();
                int latCounter = 0;
                int lonCounter = 0;
         
                for (int i = 0; i < tmp.Count; i++)
                {
                    if ((latCounter % _filterLevel == 0) &&
                         (lonCounter % _filterLevel == 0))
                    {
                        _windListFiltered.Add(tmp[i]);
                    }
                    lonCounter++;

                    if ( (i + 1 < tmp.Count) && 
                        (tmp[i].Position.Latitude != tmp[i + 1].Position.Latitude) )
                    {
                        latCounter++;
                        lonCounter = 0;
                    }
                }
            }

            //System.Diagnostics.Debug.WriteLine("-FilterOverlays");
        }

    }

}
