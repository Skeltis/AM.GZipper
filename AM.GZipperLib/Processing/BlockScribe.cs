using System;
using System.IO;
using System.Linq;
using AM.GZipperLib.Data;
using AM.GZipperLib.DataFormat;

namespace AM.GZipperLib
{
    internal sealed class BlockScribe
    {
        private string _originalFileName;
        private Int32 _blockSize;

        private bool _firstBlockScribed = false;

        public Int32 TotalBlocks { get; private set; }

        public Int64 OriginalFileSize { get; private set; }

        public static BlockScribe FromTargetFile(string fileName, Int32 blockSize)
        {
            var fileInfo = new FileInfo(fileName);
            return new BlockScribe(fileInfo.Name, fileInfo.Length, blockSize);
        }

        public static BlockScribe FromCompressedFile(string fileName)
        {
            GzipFileInfo gzInfo = GzipFileInfo.FromCompressedFile(fileName);
            return new BlockScribe(gzInfo);
        }

        public BlockScribe(string originalFileName, Int64 originalFileSize, Int32 blockSize)
        {
            _originalFileName = originalFileName;
            OriginalFileSize = originalFileSize;
            _blockSize = blockSize;
            CountBlockParams(originalFileSize, blockSize);
        }

        public BlockScribe(GzipFileInfo gzipInfo)
        {
            var originalInfo = gzipInfo.FindFirstFieldOrDefault(OriginalInfoField.SI1_OriginalInfo, OriginalInfoField.SI2_OriginalInfo) as OriginalInfoField;
            if (originalInfo == null) throw new FormatException("Invalid gzip format");

            _originalFileName = gzipInfo.OriginalFileName;
            OriginalFileSize = (Int64)originalInfo.FileSize;
            _blockSize = (Int32)originalInfo.BlockSize;
            TotalBlocks = (Int32)originalInfo.TotalBlocks;
        }

        public IBlock ScribeBlock(IBlock block)
        {
            var currInfo = GzipFileInfo.FromCompressedData(block.Data);
            Int32 headSize = (Int32)currInfo.HeaderSize;
            UInt32 dataSize = (UInt32)(block.Length - headSize - GzipFileInfo.TAIL_SIZE);
            var cutOriginHeader = block.Data.Skip(headSize).Take(block.Length - headSize).ToArray();
            var resultData = GetHeader(currInfo, (UInt32)block.BlockNumber, dataSize).Concat(cutOriginHeader).ToArray();
            block.Dispose();
            return new DataBlock(resultData, block.BlockNumber);
        }

        private byte[] GetHeader(GzipFileInfo currInfo, UInt32 blockNumber, UInt32 dataSize)
        {
            if (!_firstBlockScribed)
            {
                _firstBlockScribed = true;
                currInfo.SetOriginalFileName(_originalFileName);
                currInfo.AddExtraField(new OriginalInfoField((UInt32)_blockSize, (UInt32)TotalBlocks, (UInt64)OriginalFileSize));
            }
            currInfo.AddExtraField(new BlockInfoField(blockNumber, dataSize));
            return currInfo.CreateHeader();
        }

        private void CountBlockParams(Int64 fileSize, Int32 blockSize)
        {
            TotalBlocks = (Int32)Math.Ceiling((double)fileSize / blockSize);
        }
    }
}
