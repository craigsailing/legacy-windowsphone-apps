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
    class GRIBGroup<T> : List<T>
    {
        public string Key { get; private set; }

        public GRIBGroup(string key)
        {
            Key = key;
        }
    }

    public class GRIBDataItem
    {
        public GRIBDataItem()
        {
            DateTime = DateTime.Now;
            WindDirection = 0;
            WindSpeed = 0;
            Pressure = 0;
        }

        public DateTime DateTime { get; set; }

        public string Date
        {
            get { return DateTime.ToShortDateString(); }
        }

        public string Hour
        {
            get { return DateTime.ToString("HH"); }
        }

        public int WindDirection { get; set; }
        public int WindSpeed { get; set; }
        public int Pressure { get; set; }
    }

    class DataTableLayer
    {
        private List<GRIBDataItem> _GRIBData;
        private List<GRIBGroup<GRIBDataItem>> _GRIBDataGrouped;

        public DataTableLayer()
        {
            _GRIBDataGrouped = new List<GRIBGroup<GRIBDataItem>>();
            _GRIBData = new List<GRIBDataItem>();

            GeoCoordinate xy = App.Model.PosLastInfo;

            LoadData();

            //TODO if cant load last data on FAS/Thombsone or new GRIB need a failure path
            GroupData();
        }

        private void LoadData()
        {
            GeoCoordinate xy = App.Model.PosLastInfo;

            List<double> pressure = new List<double>();
            App.Model.Pressure(xy, pressure);

            List<double> speed = new List<double>();
            List<int> angle = new List<int>();
            App.Model.Wind(xy, angle, speed);

            for (int i = 0; i < speed.Count; i++)
            {
                GRIBDataItem item = new GRIBDataItem();

                if (speed.Count > 0 && i < speed.Count)
                {
                    item.WindDirection = angle[i];
                    item.WindSpeed = (int)speed[i];
                }
                else
                {
                    item.WindDirection = 0;
                    item.WindSpeed = 0;
                }

                if (pressure.Count > 0 && i < pressure.Count)
                    item.Pressure = (int)pressure[i] / 100;
                else
                    item.Pressure = 0;

                item.DateTime = App.Model.ForCastDateTime + new TimeSpan(i * App.Model.Interval, 0, 0);

                _GRIBData.Add(item);
            }
        }

        private void GroupData()
        {
            //Group it
            foreach (GRIBDataItem item in _GRIBData)
            {
                //if day changes create new key
                if (_GRIBDataGrouped.Count == 0 || item.Date != _GRIBDataGrouped[_GRIBDataGrouped.Count - 1].Key)
                {
                    _GRIBDataGrouped.Add(new GRIBGroup<GRIBDataItem>(item.Date));
                }

                //Add it to the current 
                if (_GRIBDataGrouped.Count > 0)
                {
                    _GRIBDataGrouped[_GRIBDataGrouped.Count - 1].Add(item);
                }
            }
        }

        public List<GRIBDataItem> Data { get { return _GRIBData; } }
        public List<GRIBGroup<GRIBDataItem>> DataGrouped { get { return _GRIBDataGrouped; } }

    }

}
