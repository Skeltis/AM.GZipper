using Microsoft.VisualBasic.Devices;
using System;

namespace AM.GZipperLib
{
    internal sealed class SystemInfoProvider
    {
        private Int32 _blockSize;
        private Int64 _upperLimit;
        private Int64 _lastAllocatedMemory;

        public Int32 LogicalCores { get; } = Environment.ProcessorCount;

        public bool CheckIsEnoughMemory()
        {
            _lastAllocatedMemory = GC.GetTotalMemory(false);
            return _lastAllocatedMemory < _upperLimit;
        }

        public SystemInfoProvider(Int32 blockSize)
        {
            _upperLimit = CountMemoryCap();
            _blockSize = blockSize;
        }

        private Int64 CountMemoryCap()
        {
            UInt64 Mem2GB = 2 * 1024U * 1024U * 1024U;
            UInt64 totalPhysMemory = new ComputerInfo().TotalPhysicalMemory;

            UInt64 memoryCapForProcess = totalPhysMemory / 2;
            memoryCapForProcess = memoryCapForProcess > Mem2GB ? Mem2GB : memoryCapForProcess;

            return (Int64)memoryCapForProcess;
        }

    }
}
