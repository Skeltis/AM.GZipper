using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using AM.GZipperLib.Data;

namespace AM.GZipperLib
{
    internal class GZipBlockCompresser : IBlockCompresser
    {
        private CompressionLevel _level;

        public GZipBlockCompresser(CompressionLevel level)
        {
            _level = level;
        }

        public IBlock Compress(IBlock data)
        {
            using (var memoryStream = new MemoryStream(data.Length))
            {
                using (var compressionStream = new GZipStream(memoryStream, _level, false))
                {
                    compressionStream.Write(data.Data, 0, data.Data.Length);
                }
                byte[] resultData = memoryStream.ToArray();

                return new DataBlock(resultData, data.BlockNumber);
            }
        }

        public IBlock Decompress(IBlock data)
        {
            using (var ms = new MemoryStream(data.Data))
            {
                using (var decompressionStream = new GZipStream(ms, CompressionMode.Decompress, true))
                {
                    int bytesRead;
                    List<byte> seq = new List<byte>();
                    byte[] buffer = new byte[data.Length];
                    do
                    {
                        bytesRead = decompressionStream.Read(buffer, 0, buffer.Length);
                        seq.AddRange(buffer.Take(bytesRead));
                    }
                    while (bytesRead > 0);

                    return new DataBlock(seq.ToArray(), data.BlockNumber);
                }
            }


        }

    }
}
