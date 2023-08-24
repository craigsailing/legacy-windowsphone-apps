using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;


namespace GribDecoder
{
    public class GribHelpers
    {
        private GribHelpers() { }

        public static double WindSpeed(double u, double v)
        {
            //wind speed in knots
            return Math.Sqrt(((u * u) + (v * v))) * 1.94384;
        }

        public static int WindAngle(double u, double v)
        {
            //Cast to int only interested in whole deg angles for wind
            return (int)(Math.Atan2(u, v) * (180 / Math.PI)) + 180;
        }

        public static int IntFrom2Bytes(byte a, byte b)
        {
            //if MSB is set 1, then the number is negative
            return (((a & 0x7f) << 8) + b) * (1 - ((a & 0x80) >> 6));
        }

        public static int IntFrom3Bytes(byte a, byte b, byte c)
        {
            //if MSB is set 1, then the number is negative
            return (((a & 0x7f) << 16) + (b << 8) + c) * (1 - ((a & 0x80) >> 6));
        }

        public static int UIntFrom2Bytes(byte a, byte b)
        {
            return ((a << 8) + b);
        }

        public static int UIntFrom3Bytes(byte a, byte b, byte c)
        {
            return ((a << 16) + (b << 8) + c);
        }

        public static int UIntFrom4Bytes(byte a, byte b, byte c, byte d)
        {
            return ((a << 24) + (b << 16) + (c << 8) + d);
        }

        public static double IBMtoFloat(byte a, byte b, byte c, byte d)
        {
            int positive, power;
            uint abspower;
            long mant;  //?Was long int?
            double value, exp;

            mant = (b << 16) + (c << 8) + d;

            if (mant == 0) return 0.0;

            if ((a & 0x80) == 0)
                positive = 1;
            else
                positive = 0;

            power = (int)(a & 0x7f) - 64;
            abspower = (uint)Math.Abs(power);

            /* calc exp */
            exp = 16.0;
            value = 1.0;
            while (abspower != 0)
            {
                if ((abspower & 1) == 1)
                {
                    value *= exp;
                }
                exp = exp * exp;
                abspower >>= 1;
            }

            if (power < 0) value = 1.0 / value;
            value = value * mant / 16777216.0;
            if (positive == 0) value = -value;
            return value;
        }

        public static double WrapLongitude(double lon)
        {
            if (Math.Abs(lon) <= 180)
            {
                return lon;
            }

            //East Hemph
            if (lon < -180)
            {
                return lon + 360;
            }

            //West Hemph
            if (lon > 180)
            {
                return lon - 360;
            }

            return lon;
        }
    }

    // Enums For Grib Spec Mappings
    public enum TimeUnit : byte
    {
        MINUTE = 0,
        HOUR = 1,
        DAY = 2,
        MONTH = 3,
        YEAR = 4,
        DECADE = 5,
        NORMAL = 6,
        CENTURY = 7,
        HOURS_3 = 10,
        HOURS_6 = 11,
        HOURS_12 = 12,
        MINUTES_15 = 13,
        MINUTES_30 = 14,
        SECOND = 254,
    }

    public enum Center : byte
    {
        [Description("US National Weather Service - NCEP WMC")]
        USNOAANCEP = 7,
        [Description("US National Weather Service - NWSTG (WMC)")]
        USNOAANWSTG = 8,
        [Description("US National Weather Service - Other (WMC)")]
        USNOAAOther = 9,
        [Description("Norrkoping1")]
        NORRK1 = 82,
        [Description("Norrkoping2")]
        NORRK2 = 83,
        [Description("French Weather Service - Toulouse1")]
        FWST1 = 84,
        [Description("French Weather Service - Toulouse2")]
        FWST2 = 85,
        [Description("European Center for Medium-Range Weather Forecasts (RSMC)")]
        ECMRWFRMS = 98
    }

    public enum Paramater : byte
    {
        //Values Supported Currently for GRIB parameters
        [Description("Pressure Mean Sea Level in Pa")]
        PRMSL = 2,
        [Description("Temperature in K")]
        TMP = 11,
        [Description("u-component of Wind in m/s")]
        UGRD = 33,
        [Description("v-component of Wind in m/s")]
        VGRD = 34,
        [Description("Total Precipitation in kg/m^2")]
        ACPC = 61,
        [Description("Total Cloud cover as %")]
        TCDC = 71,
    }

    public enum Units
    {

    }

    public enum TypeLevel : byte
    {
        [Description("Mean Sea Level")]
        MSL = 102,
        [Description("Specified hieght level above the ground in meters")]
        HTGL = 105,
    }
}
