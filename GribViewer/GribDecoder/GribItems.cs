
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GribDecoder
{
    public class GribItem
    {

        #region private members

        private Stream _fileStream;
        
        //Section Lengths
        private int _pDSLenth = 0;
        private int _gDSLenth = 0;
        private int _bDSLenth = 0;
        private int _bMSLenth = 0;
        private double[] _gbData = null;

        #endregion

        public bool Decode(Stream dataFile)
        {
            bool retVal = true;
            _fileStream = dataFile;
            try
            {
                //Decode Section 0
                retVal = DecodeSection0();

                if (retVal == true)
                {
                    //Decode Section 1
                    DecodeSection1();

                    //Decode GDS Section
                    DecodeGDSSection();

                    //Decod BMS Section
                    DecodeBMSection();

                    //Decode Binary Section
                    DecodeBinarySection();
                }

            } //TODO Catch here
            finally
            {
                //Dont hold a ref to this stream
                _fileStream = null;
            }

            return retVal;
        }

        #region Properties

        //PDS
        public int GribItemLength { get; private set; }                     //Section 0 - 5, Length of grib from GRIB-7777
        
        public Center ForeCastCenter { get; private set; }
        public byte GeneratingProcess { get; private set; }
        public byte GridIdentification { get; private set; }
        
        public DateTime ForeCastTime { get; private set; }
        public TimeUnit ForeCastTimeUnit { get; private set; }              //TODO Dont handle if this is not hours NB!
        public int ForeCastPeriodN1 { get; private set; }
        public int ForeCastPeriodN2 { get; private set; }
        public int TimeRangeIndicator { get; private set; }
        public int ForeCastTimeOffSet { get; private set; }                 //TODO Dont handle if this is not hours NB!

        public Paramater Paramater { get; private set; }
        public TypeLevel TypeLevel { get; private set; }
        public int LevelValue { get; private set; }
        public bool HasBMSSection { get; private set; }
        public bool HasGDSSection { get; private set; }
        public int DecimalScaleFactor { get; private set; }

        //BMS Sections
        public int BMSXY { get; private set; }
        public byte[] BMSMap { get; private set; }


        //BDS Section
        public int BDSBinaryScaler { get; private set; }
        public double BDSReferanceValue { get; private set; }
        public int DataPointNBits { get; private set; }
        
        //GDS 
        public int Lat0 { get; private set; }
        public int Lon0 { get; private set; }
        public int Lat1 { get; private set; }
        public int Lon1 { get; private set; }
        public int LatNx { get; private set; }
        public int LonNy { get; private set; }
        public int LatR { get; private set; }
        public int LonR { get; private set; }
        public bool DirInc { get; private set; }
        public int iDir { get; private set; }
        public int jDir { get; private set; }

        //Data Points
        public double[] Data
        {
            get
            {
                return _gbData;
            }
        }

        #endregion
        
        private void DecodeBinarySection()
        {
            //BDS Section lenthg includes these 3 bytes
            byte[] bDSLenth = new byte[3];
            _fileStream.Read(bDSLenth, 0, 3);

            //Read the rest of the BDS section
            _bDSLenth = (bDSLenth[0] << 16) + (bDSLenth[1] << 8) + bDSLenth[2];
            byte[] bDSSection = new byte[_bDSLenth - 3];
            _fileStream.Read(bDSSection, 0, _bDSLenth - 3);

            //First 12 bytes are metadata then the encoded data.
            byte bdsFlags = bDSSection[0];

            //5-6 is the binary scale factor E (neg is MSB set in Byte 5)
            BDSBinaryScaler = GribHelpers.IntFrom2Bytes(bDSSection[1], bDSSection[2]);

            //7-10
            BDSReferanceValue = GribHelpers.IBMtoFloat(bDSSection[3], bDSSection[4], bDSSection[5], bDSSection[6]);

            DataPointNBits = bDSSection[7];

            //bool isIntVal = Convert.ToBoolean(bdsFlags & 2);

            if ((bdsFlags & 128) == 128)
            {
                //Complex Packing not supported
                throw new InvalidDataException("Harmonic Packing not supported");
            } 

            if ((bdsFlags & 64) == 64)
            {
                //Complex Packing not supported
                throw new InvalidDataException("Complex Packing not supported");
            }

            if ((bdsFlags & 16) == 16)
            {
                throw new InvalidDataException("Don,t support BDS with Byte 14 data");
            }

            //Simple Packing Grid Point data (Data starts in byte 12 and continue);
            if ((bdsFlags & 128) == 0)
            {
                //OK can handle this
                //UNPACK BDS data 
                UnPackBDSData(bDSSection, 8);
            }

            //At end of BDS is the END of the Grib Item 7777;
            byte[] gribEndTag = new byte[4];
            _fileStream.Read(gribEndTag, 0, 4);

            if (!(gribEndTag[0] == '7' &&                       //End tag of GRIB
                gribEndTag[1] == '7' &&
                gribEndTag[2] == '7' &&
                gribEndTag[3] == '7'))
            {
                throw new InvalidDataException("Did not find GRIB end tag 7777 as expected data is corrupted");
            }
        }

        private void UnPackBDSData(byte[] data, int offset)
        {
            int n = LatNx * LonNy;
            _gbData = new double[n];
            
            double temp = Math.Pow(10.0, -DecimalScaleFactor);
            double referance =  temp*BDSReferanceValue;
            double scale = temp*Math.Pow(2.0, BDSBinaryScaler);

            int i, t_bits = 0; 
            int mask_idx; 
            uint tbits = 0, jmask = 0; 
            uint bbits = 0;

            /* assume integer has 32+ bits */
            if (DataPointNBits <= 25) 
            {
                jmask = (uint)((1 << DataPointNBits) - 1);
                t_bits = 0;
            }

            if (HasBMSSection) 
            {
                //Need to merge with a bit map at this point some data cells have no data.
                uint[] map_masks = {128, 64, 32, 16, 8, 4, 2, 1};
                int mapIndex = 0;
                int index = offset;
                for (i = 0; i < n; i++)
			    {
				    /* check bitmap */
				    mask_idx = i & 7;
                    if (mask_idx == 0) bbits = BMSMap[mapIndex++];
				    if ((bbits & map_masks[mask_idx]) == 0) 
				    {
                        _gbData[i] = 0; //UNDEFINED;
					    continue;
				    }

                    while (t_bits < DataPointNBits)
				    {
                        tbits = (tbits * 256) + data[index];
					    t_bits += 8;
                        index++;
				    }

                    t_bits -= DataPointNBits;
                    _gbData[i] = referance + scale * ((tbits >> t_bits) & jmask);
			    }

                //Release this map so that we dont hold a ref to this for the lifetime only needed for decoding.
                BMSMap = null;
            }
            else
            {
                int index = offset;
                for (i = 0; i < n; i++) 
                {
		            if (DataPointNBits - t_bits > 8) 
                    {
                        tbits = (tbits << 16) | (uint)(data[index] << 8) | (uint)(data[index+1]);
		                index += 2;
                        t_bits += 16;
		            }

                    //byte current = data[index];
                    while (t_bits < DataPointNBits)
                    {
                        tbits = (tbits * 256) + data[index];
                        t_bits += 8;
                        index++;
                    }

                    t_bits -= DataPointNBits;
                    _gbData[i] = (tbits >> t_bits) & jmask;
                 }

	            /* at least this vectorizes :) */
	            for (i = 0; i < n; i++) 
                {
		            _gbData[i] = referance + scale*_gbData[i];
	            }
            }
        }
        
        private void DecodeGDSSection()
        {
            if (!HasGDSSection)
            {   //IF GDS flag is set & 128 then decode GDS sections 
                throw new InvalidDataException("Don't have GDS Section bit set should not try to decode this.");
            }

            byte[] gDSLenth = new byte[3];
            _fileStream.Read(gDSLenth, 0, 3);
            _gDSLenth = GribHelpers.UIntFrom3Bytes(gDSLenth[0], gDSLenth[1], gDSLenth[2]);
            byte[] gDSSection = new byte[_gDSLenth - 3];
            _fileStream.Read(gDSSection, 0, _gDSLenth - 3);

            if ((gDSSection[1] == 255 || gDSSection[1] == 33) &&
                 gDSSection[2] == 0)        //Byte5 is set to 255 Byte 6 is 0 Lat/Lon Grid Equidistant Cylindrical, or Plate Carree Proj
            {

                LonNy = GribHelpers.UIntFrom2Bytes(gDSSection[3], gDSSection[4]);                       //#n W to E
                LatNx = GribHelpers.UIntFrom2Bytes(gDSSection[5], gDSSection[6]);                       //#n N to S
                Lat0 = GribHelpers.IntFrom3Bytes(gDSSection[7], gDSSection[8], gDSSection[9]);          //IF MSB set then it is South
                Lon0 = GribHelpers.IntFrom3Bytes(gDSSection[10], gDSSection[11], gDSSection[12]);       //IF MSB set then it is North

                DirInc = Convert.ToBoolean(gDSSection[13] & 0x80);                                      //Direction of increment given

                Lat1 = GribHelpers.IntFrom3Bytes(gDSSection[14], gDSSection[15], gDSSection[16]);       //IF MSB set then it is South
                Lon1 = GribHelpers.IntFrom3Bytes(gDSSection[17], gDSSection[18], gDSSection[19]);       //IF MSB set then it is North
                
                LonR = GribHelpers.UIntFrom2Bytes(gDSSection[20], gDSSection[21]);                      //resolution is 500mDeg = 0.5 deg res grib
                LatR = GribHelpers.UIntFrom2Bytes(gDSSection[22], gDSSection[23]);                      //resolution is 1000mDeg = 1 deg res grib

                iDir = (gDSSection[24] & 128) == 0 ? 1 : -1;                                            //MSB 0 = +i 1 = -i (west to east), MSB-1 0 = -j 1 = +j south to north 
                jDir = (gDSSection[24] & 64) == 1 ? 1 : -1;
            }
            else
            {
                throw new InvalidDataException("Unknown GDS data");
            }
        }

        private void DecodeBMSection()
        {
            //If there is no section return nothing needed
            if (!HasBMSSection) { return; }

            byte[] bMSLenth = new byte[3];
            _fileStream.Read(bMSLenth, 0, 3);
            _bMSLenth = GribHelpers.UIntFrom3Bytes(bMSLenth[0], bMSLenth[1], bMSLenth[2]);

            byte[] bMSSection = new byte[_bMSLenth - 3];
            _fileStream.Read(bMSSection, 0, _bMSLenth - 3);

            int unusedBits = bMSSection[0];
            int stdMap = GribHelpers.UIntFrom2Bytes(bMSSection[1], bMSSection[2]);

            if (stdMap != 0)
            {
                throw new InvalidDataException("BMS Std BitMap not supported"); 
            }

            //Store just the BMS Map
            BMSXY = (stdMap == 0) ? (_bMSLenth * 8 - 48 - unusedBits) : 0;
            BMSMap = new byte[_bMSLenth - 6];
            Array.Copy(bMSSection, 3, BMSMap, 0, _bMSLenth - 6);
        }

        private void DecodeSection1()
        {
            //http://www.nco.ncep.noaa.gov/pmb/docs/on388/section1.html
            byte[] pDSLenth = new byte[3];
            _fileStream.Read(pDSLenth, 0, 3);

            //This should be 28 bytes but can vary the spec says ?
            _pDSLenth = GribHelpers.UIntFrom3Bytes(pDSLenth[0], pDSLenth[1], pDSLenth[2]);

            //Read PDS Section
            byte[] pdsSection = new byte[_pDSLenth - 3];
            _fileStream.Read(pdsSection, 0, _pDSLenth - 3);

            ForeCastCenter = (Center)pdsSection[1];
            GeneratingProcess = pdsSection[2];                            //81 is GFS
            GridIdentification = pdsSection[3];                           //255 is the Grid is not described and is then in GDS sections

            HasGDSSection = Convert.ToBoolean(pdsSection[4] & 128);       //Has GDS Section (MSB of the BYTE)
            HasBMSSection = Convert.ToBoolean(pdsSection[4] & 64);        //Has BDS Section (MSB - 1 of he BYTE) rest of the bits are reserved

            if (HasBMSSection)
            {
                //throw new NotImplementedException("Do Not currently support GRIBS with BMS section");
            }

            if (!HasGDSSection)
            {
                throw new NotImplementedException("Currently can only decode GRIB's with 255 and GDS Sections");
            }

            Paramater = (Paramater)pdsSection[5];                         //What is in this GRIB uWind vWind Press Rain ETC
            TypeLevel = (TypeLevel)pdsSection[6];                         //Level at which the measurement is 
            LevelValue = GribHelpers.UIntFrom2Bytes(pdsSection[7], pdsSection[8]);
      
            ForeCastTime = new DateTime(2000 + pdsSection[9], pdsSection[10], pdsSection[11], pdsSection[12], pdsSection[13], 0, DateTimeKind.Utc);
            ForeCastTimeUnit = (TimeUnit)pdsSection[14];
            ForeCastPeriodN1 = pdsSection[15];
            ForeCastPeriodN2 = pdsSection[16];
            TimeRangeIndicator = pdsSection[17];

            //Handle the common time formats http://www.nco.ncep.noaa.gov/pmb/docs/on388/table5.html
            if (TimeRangeIndicator == 0)                //Forecast product valid for reference time + P1
            {
                ForeCastTimeOffSet = ForeCastPeriodN1;
            }

            if (TimeRangeIndicator == 4)                //Accumulation reference time + P1 to reference time + P2
            {
                ForeCastTimeOffSet = ForeCastPeriodN2;
            }

            if (TimeRangeIndicator == 10)               //P1 in Byte 15 and 16
            {
                ForeCastTimeOffSet = GribHelpers.IntFrom2Bytes(pdsSection[15], pdsSection[16]);
            }
            
            DecimalScaleFactor = GribHelpers.IntFrom2Bytes(pdsSection[23], pdsSection[24]);

            if (_pDSLenth > 28)
            {
                throw new NotImplementedException("Don't Support GDS > 28 currently");
            }
        }

        private bool DecodeSection0()
        {
            byte[] section0 = new Byte[8];                  //Section is 8 bytes long
            
            //Strip any wite space
            _fileStream.Read(section0, 0, 1);
            while (_fileStream.Position < _fileStream.Length && section0[0] != 'G')
            {
                _fileStream.Read(section0, 0, 1);
            }

            if (_fileStream.Position == _fileStream.Length)
            {
                return false;
            }

            _fileStream.Read(section0, 1, 7);
            if (section0[0] == 'G' &&                       //Header GRIB
                section0[1] == 'R' &&
                section0[2] == 'I' &&
                section0[3] == 'B')
            {
                if (section0[7] == 1)                       //Version Check only V1 supported currently
                {
                    GribItemLength = GribHelpers.UIntFrom3Bytes(section0[4], section0[5], section0[6]);
                    //TODO small gribs may need a ECMWF hack
                    return true;
                }
                else
                {
                    throw new InvalidDataException("Invalid Section 0 GRIB Version not 1");
                }
            }
            else
            {
                throw new InvalidDataException("Invalid Section 0");
            }
        }
    }
}
