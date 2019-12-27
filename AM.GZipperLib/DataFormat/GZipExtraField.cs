using System;
using System.Collections.Generic;

namespace AM.GZipperLib.DataFormat
{
    public class GZipExtraField
    {
        public byte SI1 { get; }
        public byte SI2 { get; }
        public UInt16 FieldDataLength { get; }
        public UInt16 FieldTotalLength { get; }
        public byte[] Data { get; }


        public GZipExtraField(byte si1, byte si2, byte[] data)
        {
            SI1 = si1;
            SI2 = si2;
            FieldDataLength = (UInt16)data.Length;
            Data = data;
            FieldTotalLength = (UInt16)(FieldDataLength + 4);
        }

        public byte[] ToByteArray()
        {
            var output = new List<byte>();
            output.AddRange(new[] { SI1, SI2 });
            output.AddRange(Utilities.GetBytesOfUInt<UInt16>(FieldDataLength));
            output.AddRange(Data);
            return output.ToArray();
        }

        public override bool Equals(object obj)
        {
            var compar = obj as GZipExtraField;
            if (compar == null) return false;
            bool result = 
                compar.SI1 == SI1 && 
                compar.SI2 == SI2 && 
                compar.FieldDataLength == FieldDataLength;

            int counter = 0;

            while (result && counter < Data.Length)
            {
                result = result && compar.Data[counter] == Data[counter];
                counter++;
            }
            return result;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
