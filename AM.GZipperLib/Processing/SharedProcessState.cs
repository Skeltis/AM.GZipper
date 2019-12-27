using System;
using System.Threading;

namespace AM.GZipperLib
{
    internal sealed class SharedProcessState
    {
        private Int32 _readInProcess = 0;
        private Int32 _writeInProcess = 0;

        private Int32 _blocksToRead;
        private Int32 _blocksToProcess;
        private Int32 _blocksToWrite;

        private Int32 _lastWrittenBlock;
        private bool[] _blocksProcessedStates;
        private Int32 _processedBlocks;

        private SystemInfoProvider _sysInformer;

        public bool IsEnoughMemory => _sysInformer.CheckIsEnoughMemory();

        public bool IsReadInProcess => _readInProcess == 1;
        public bool IsWriteInProcess => _writeInProcess == 1;
        public Int32 BlocksToProcessCount => _blocksToProcess;
        public bool HasDataToRead => _blocksToRead != 0;
        public bool HasDataToWrite => _blocksToWrite > 0 && _processedBlocks > 0 && _blocksProcessedStates[_lastWrittenBlock + 1];


        public Int32 LastWrittenBlock => _lastWrittenBlock;

        public SharedProcessState(Int32 totalBlocks, Int32 blockSize)
        {
            _sysInformer = new SystemInfoProvider(blockSize);
            _blocksToRead = totalBlocks;
            _blocksToWrite = totalBlocks;
            _lastWrittenBlock = 0;
            _blocksProcessedStates = new bool[totalBlocks + 1];
        }

        
        public void InformReadInProcess() => Interlocked.Exchange(ref _readInProcess, 1);
        public void InformBlockReaded()
        {
            Interlocked.Decrement(ref _blocksToRead);
            Interlocked.Increment(ref _blocksToProcess);
        }
        public void InformReadCompleted() => Interlocked.Exchange(ref _readInProcess, 0);

        
        public void InformWriteInProcess() => Interlocked.Exchange(ref _writeInProcess, 1);
        public void InformBlockWritten()
        {
            Interlocked.Increment(ref _lastWrittenBlock);
            Interlocked.Decrement(ref _blocksToWrite);
        }
        public void InformWriteCompleted() => Interlocked.Exchange(ref _writeInProcess, 0);

        public void ReserveBlockForProcessing() => Interlocked.Decrement(ref _blocksToProcess);
        public void InformBlockProcessed(Int32 number)
        {
            _blocksProcessedStates[number] = true;
            Interlocked.Increment(ref _processedBlocks);
        }
        public void ReleaseBlockForProcessing() => Interlocked.Increment(ref _blocksToProcess);



    }
}
