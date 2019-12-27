using System;
using System.Collections.Generic;

namespace AM.GZipperLib.DataFormat
{
    public class BlockInfoField : GZipExtraField
    {
        public const byte SI1_BlockNumber = 0xA0;
        public const byte SI2_BlockNumber = 0xC0;

        public UInt32 BlockNumber { get; private set; }
        public UInt32 DataSize { get; private set; }

        public BlockInfoField(byte[] data) : 
            base(SI1_BlockNumber, SI2_BlockNumber, data)
        {
            ParseFromData();
        }

        public BlockInfoField(UInt32 blockNumber, UInt32 dataSize) :
            base(SI1_BlockNumber, SI2_BlockNumber, GetBytesOfContent(blockNumber, dataSize))
        {
            BlockNumber = blockNumber;
            DataSize = dataSize;
        }

        private void ParseFromData()
        {
            BlockNumber = Utilities.GetUIntFromBytes<UInt32>(Data, 0);
            DataSize = Utilities.GetUIntFromBytes<UInt32>(Data, 4);
        }

        public override bool Equals(object obj)
        {
            BlockInfoField compar = obj as BlockInfoField;
            if (compar == null) return false;
            return base.Equals(obj);
        }

        public override int GetHashCode() => base.GetHashCode();

        private static byte[] GetBytesOfContent(UInt32 blockNumber, UInt32 dataSize)
        {
            var output = new List<byte>();
            output.AddRange(Utilities.GetBytesOfUInt<UInt32>(blockNumber));
            output.AddRange(Utilities.GetBytesOfUInt<UInt32>(dataSize));
            return output.ToArray();
        }
    }
}
