using System;
using System.Collections.Generic;

namespace AM.GZipperLib.DataFormat
{
    public class OriginalInfoField : GZipExtraField
    {
        public const byte SI1_OriginalInfo = 0xA0;
        public const byte SI2_OriginalInfo = 0xC1;

        public UInt32 BlockSize { get; private set; }
        public UInt32 TotalBlocks { get; private set; }
        public UInt64 FileSize { get; private set; }

        public OriginalInfoField(UInt32 blockSize, UInt32 totalBlocks, UInt64 fileSize) : 
            base(SI1_OriginalInfo, SI2_OriginalInfo, GetBytesOfContent(blockSize, totalBlocks, fileSize))
        {
            BlockSize = blockSize;
            TotalBlocks = totalBlocks;
            FileSize = fileSize;
        }

        public OriginalInfoField(byte[] data) : 
            base(SI1_OriginalInfo, SI2_OriginalInfo, data)
        {
            SetFieldsFromData();
        }

        private void SetFieldsFromData()
        {
            BlockSize = Utilities.GetUIntFromBytes<UInt32>(Data, 0);
            TotalBlocks = Utilities.GetUIntFromBytes<UInt32>(Data, 4);
            FileSize = Utilities.GetUIntFromBytes<UInt64>(Data, 8);
        }

        private static byte[] GetBytesOfContent(UInt32 blockSize, UInt32 totalBlocks, UInt64 fileSize)
        {
            var output = new List<byte>();
            output.AddRange(Utilities.GetBytesOfUInt<UInt32>(blockSize));
            output.AddRange(Utilities.GetBytesOfUInt<UInt32>(totalBlocks));
            output.AddRange(Utilities.GetBytesOfUInt<UInt64>(fileSize));
            return output.ToArray();
        }

        public override bool Equals(object obj)
        {
            OriginalInfoField compar = obj as OriginalInfoField;
            if (compar == null) return false;
            return 
                compar.BlockSize == compar.BlockSize &&
                compar.TotalBlocks == compar.TotalBlocks &&
                compar.FileSize == compar.FileSize &&
                base.Equals(obj);
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
