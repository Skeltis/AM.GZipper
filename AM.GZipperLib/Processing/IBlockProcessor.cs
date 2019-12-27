using System;

namespace AM.GZipperLib
{
    internal interface IBlockProcessor : IDisposable
    {
        void ProcessBlock();
        void ReadBlock();
        void SetProgressInformer(IProgressInformer informer);
        void WriteBlock();
    }
}