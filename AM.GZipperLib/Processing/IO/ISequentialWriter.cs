using System;
using AM.GZipperLib.Data;

namespace AM.GZipperLib
{
    public interface ISequentialWriter : IDisposable
    {
        void MoveNext();
        void Reset();
        void WriteBlock(IBlock block);
    }
}