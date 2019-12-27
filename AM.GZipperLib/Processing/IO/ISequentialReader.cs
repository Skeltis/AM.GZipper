using System;
using AM.GZipperLib.Data;

namespace AM.GZipperLib
{
    public interface ISequentialReader : IDisposable
    {
        bool MoveNext();
        IBlock ReadBlock();
        void Reset();
    }
}