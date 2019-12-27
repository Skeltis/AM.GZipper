using System;

namespace AM.GZipperLib.Data
{
    public interface IBlock : IDisposable
    {
        Int32 BlockNumber { get; }
        byte[] Data { get; }
        Int32 Length { get; }

    }
}
