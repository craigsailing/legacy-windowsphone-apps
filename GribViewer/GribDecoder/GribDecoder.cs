using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace GribDecoder
{
    public class GribDecoder 
    {
        private List<GribItem> _gribItems;

        //user level data of griditems
        private List<Paramater> _paramatersInGrib;
        private Dictionary<Paramater, List<GeoCoordinate>> _geoGrids;

        #region Peoperties

        public int ForeCastEndHours { get; private set; }
        public int ForCastIntervaLCount { get; private set; }
        public DateTime ForeCastTime { get; private set; }

        public Center ForeCastCenter { get; private set; }
        public double ResolutionLat { get; private set; }
        public double ResolutionLon { get; private set; }
        public bool GribLoaded { get; private set; }
        
        public List<GeoCoordinate> Grid(Paramater param)
        {
            if (_geoGrids.ContainsKey(param))
            {
                return _geoGrids[param];
            }
            else
            {
                throw new ApplicationException("Grib Parameter is not in grid collection, Grib Paramater = " + param);
            }
        }

        #endregion

        public GribDecoder()
        {
            _gribItems = new List<GribItem>();

            _paramatersInGrib = new List<Paramater>();

            _geoGrids = new Dictionary<Paramater, List<GeoCoordinate>>();
        }

        /// <summary>
        /// Decode
        /// Implements the Main Grib Decode loop
        /// </summary>
        /// <param name="gribFile"></param>
        public async Task<bool> Decode(string gribFile)
        {  
            GribLoaded = false;
            StorageFile file = await StorageFile.GetFileFromPathAsync(Path.Combine(ApplicationData.Current.LocalFolder.Path, gribFile));

            //Decode from the stream and release the file. GribItem does not hold a referance to the stream
            using (Stream dataReader = await file.OpenStreamForReadAsync())
            {
                while (dataReader.CanRead && dataReader.Position < dataReader.Length)
                {
                    GribItem data = new GribItem();
                    if (data.Decode(dataReader) == true)
                    {
                        if ( !(data.Paramater == Paramater.PRMSL ||
                             data.Paramater == Paramater.UGRD ||
                             data.Paramater == Paramater.VGRD) )
                        {
                            //Skip the rest
                            data = null;
                            continue;
                        }

                        _gribItems.Add(data);

                        //Generate the global data
                        //Get the forcast time for the file
                        //Store quick index into the data paramater tyes in the grib data set.
                        if (!_paramatersInGrib.Contains(data.Paramater))
                        {
                            _paramatersInGrib.Add(data.Paramater);
                            _geoGrids.Add(data.Paramater, BuildGeoGrid(data));
                        }

                        if (ForeCastTime == DateTime.MinValue && data.ForeCastTime != null)
                        {
                            ForeCastTime = data.ForeCastTime;
                        }

                        //Load the max n period from the forcast in all the attached gribs
                        if (ForeCastEndHours <= data.ForeCastTimeOffSet)
                        {
                            ForeCastEndHours = data.ForeCastTimeOffSet;
                        }

                        //Get the resolution of the grib in degrees assume currently they all have the same resolution
                        if (data.LonR != 0 && data.LatR != 0)
                        {
                            ResolutionLat = data.LatR / 1000.0;
                            ResolutionLon = data.LonR / 1000.0;
                        }
                    }
                }

                if (_gribItems.Count > 0)
                {
                    ForCastIntervaLCount = CountOfParameter(_paramatersInGrib[0]);
                    GribLoaded = true;
                }
                return false;
            }

        }

        public int CountOfParameter(Paramater param)
        {
            //How many itterations are there of a specific paramanter
            return _gribItems.Count(n => n.Paramater == param);
        }
        
        public void DecodeWind(int forcastIteration, List<int> angleList, List<double> speedList)
        {
            //Itterate on UGRD and VDRG
            List<GribItem> SpeedU = new List<GribItem>(from nGribItem in _gribItems where nGribItem.Paramater == Paramater.UGRD orderby nGribItem.ForeCastTimeOffSet ascending select nGribItem);
            List<GribItem> SpeedV = new List<GribItem>(from nGribItem in _gribItems where nGribItem.Paramater == Paramater.VGRD orderby nGribItem.ForeCastTimeOffSet ascending select nGribItem);

            int x = forcastIteration;
            if (forcastIteration >= SpeedU.Count)
            {
                //Passed the end protect this.
                return;
            }

            for (int i = 0; i < SpeedU[x].Data.Length; i++)
            {
                double speed = GribHelpers.WindSpeed(SpeedU[x].Data[i], SpeedV[x].Data[i]);
                int angle = GribHelpers.WindAngle(SpeedU[x].Data[i], SpeedV[x].Data[i]);
                speedList.Add(speed);
                angleList.Add(angle);
            }
        }

        public void DecodeWind(GeoCoordinate pos, List<int> angleList, List<double> speedList)
        {
            int geoIndex = Grid(Paramater.UGRD).IndexOf(pos); 

            //Itterate on UGRD and VDRG
            List<GribItem> SpeedU = new List<GribItem>(from nGribItem in _gribItems where nGribItem.Paramater == Paramater.UGRD orderby nGribItem.ForeCastTimeOffSet ascending select nGribItem);
            List<GribItem> SpeedV = new List<GribItem>(from nGribItem in _gribItems where nGribItem.Paramater == Paramater.VGRD orderby nGribItem.ForeCastTimeOffSet ascending select nGribItem);

            for (int x = 0; x < SpeedU.Count; x++)
            {
                double speed = GribHelpers.WindSpeed(SpeedU[x].Data[geoIndex], SpeedV[x].Data[geoIndex]);
                int angle = GribHelpers.WindAngle(SpeedU[x].Data[geoIndex], SpeedV[x].Data[geoIndex]);
                speedList.Add(speed);
                angleList.Add(angle);
            }
        }

        public void DecodePressure(int forcastIteration, List<double> pressureList)
        {
            List<GribItem> pressure = new List<GribItem>(from nGribItem in _gribItems where nGribItem.Paramater == Paramater.PRMSL orderby nGribItem.ForeCastTimeOffSet ascending select nGribItem);

            int x = forcastIteration;
            if (forcastIteration >= pressure.Count)
            {
                //Passed the end protect this.
                return;
            }

            for (int i = 0; i < pressure[x].Data.Length; i++)
            {
                pressureList.Add(pressure[x].Data[i]);
            }
        }

        public void DecodePressure(GeoCoordinate pos, List<double> pressureList)
        {
            int geoIndex = Grid(Paramater.UGRD).IndexOf(pos); 

            List<GribItem> pressure = new List<GribItem>(from nGribItem in _gribItems where nGribItem.Paramater == Paramater.PRMSL orderby nGribItem.ForeCastTimeOffSet ascending select nGribItem);

            foreach (GribItem item in pressure)
            {
                pressureList.Add(item.Data[geoIndex]);
            }
        }

        private void DecodePrecipiation()
        {
            List<GribItem> presure = new List<GribItem>(from nGribItem in _gribItems where nGribItem.Paramater == Paramater.ACPC orderby nGribItem.ForeCastTimeOffSet ascending select nGribItem);
            throw new NotImplementedException("Have not implemented precipitation decoding");
        }

        private List<GeoCoordinate> BuildGeoGrid(GribItem data)
        {
            List<GeoCoordinate> grid = new List<GeoCoordinate>();

            int yInc = 1;
            if (data.Lat0 > data.Lat1)
            {
                yInc = -1;
            }

            int xInc = 1;
            if (data.Lon0 > data.Lon1)
            {
                xInc = -1;
                if (data.Lon0 > 0 && data.Lon1 < 0) { xInc = 1; }       //Special case the 180 lon W and E of 180
            }

            for (int i = 0; i < data.Data.Length; i++)
            {
                double lat = data.Lat0 + ((int)(i / data.LonNy) * data.LatR * yInc);        //The Grid is a single array so use mod on lon and / on lat rounding down.
                double lon = data.Lon0 + ((i % data.LonNy) * data.LonR * xInc);

                if (lon > 180000)  { lon -= 360000; }
                if (lon < -180000) { lon += 360000; }

                grid.Add(new GeoCoordinate(lat / 1000.0, lon / 1000.0));
            }
            return grid;
        }

        public static GeoCoordinate GridCenter(GribItem data)
        {
            int LatCenter = 0;
            int LonCenter = 0;
            if (data.Lat0 > data.Lat1)
            {
                LatCenter = data.Lat1 + ((data.Lat0 - data.Lat1) / 2);
            }
            else
            {
                LatCenter = data.Lat0 + ((data.Lat1 - data.Lat0 / 2));
            }

            if (data.Lon0 > data.Lon1)
            {
                LonCenter = data.Lon1 + ((data.Lon0 - data.Lon1) / 2);
            }
            else
            {
                LonCenter = data.Lon0 + ((data.Lon1 - data.Lon0) / 2);
            }

            return new GeoCoordinate(LatCenter, LonCenter);
        }

        public bool IsParamaterInGrib(Paramater param)
        {
            return _paramatersInGrib.Contains(param);
        }
               
    }



}
