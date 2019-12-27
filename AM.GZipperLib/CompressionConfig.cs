using System;
using System.IO.Compression;
namespace AM.GZipperLib
{
    public class CompressionConfig
    {
        public string InputFile { get; }
        public string OutputFile { get; }
        public CompressionLevel Level { get; } = CompressionLevel.Optimal;
        public CompressionMode Mode { get; } = CompressionMode.Compress;
        public Int32 BlockSize { get; } = 1024 * 1024;

        public CompressionConfig(string inputFile, string outputFile, CompressionMode mode)
        {
            InputFile = inputFile;
            OutputFile = outputFile;
            Mode = mode;
        }
    }
}
