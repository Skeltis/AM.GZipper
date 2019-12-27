using System;
using System.IO.Compression;
using System.Threading;
using AM.GZipperLib.DecisionTree;

namespace AM.GZipperLib
{
    public class TaskDispatcherFabric
    {
        private BlockScribe _scribe;
        private SystemInfoProvider _sysInfoProvider;
        private SharedProcessState _stateKeeper;
        private SequentialBlockProcessor _blockProcessor;
        private BlockTaskDispatcher _taskDispatcher;
        private IDecisionNode _decisionStrategy;

        public IBlockTaskDispatcher CreateTaskDispatcher(CompressionConfig config, IProgressInformer informer)
        {
            _scribe = InitScribe(config);
            _sysInfoProvider = new SystemInfoProvider(config.BlockSize);
            _stateKeeper = new SharedProcessState(_scribe.TotalBlocks, config.BlockSize);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            _blockProcessor = new SequentialBlockProcessor(config, _scribe, _stateKeeper, cancellationTokenSource.Token);
            _blockProcessor.SetProgressInformer(informer);
            _decisionStrategy = GetDecisionStrategy(config);

            _taskDispatcher = new BlockTaskDispatcher(
                _blockProcessor, 
                _decisionStrategy, 
                _stateKeeper,
                cancellationTokenSource,
                _sysInfoProvider.LogicalCores);

            return _taskDispatcher;
        }

        private BlockScribe InitScribe(CompressionConfig config)
        {
            return (config.Mode == CompressionMode.Compress) ?
                BlockScribe.FromTargetFile(config.InputFile, config.BlockSize) :
                BlockScribe.FromCompressedFile(config.InputFile);
        }

        private IDecisionNode GetDecisionStrategy(CompressionConfig config)
        {
            return (config.Mode == CompressionMode.Compress) ?
                BuildDecisionGraphForCompression() :
                BuildDecisionGraphForDecompression();
        }

        private IDecisionNode BuildDecisionGraphForCompression()
        {
            Func<bool> isEnoughMemory = () => _stateKeeper.IsEnoughMemory;
            Func<bool> isReadInProcess = () => _stateKeeper.IsReadInProcess;
            Func<bool> isWritingInProcess = () => _stateKeeper.IsWriteInProcess;

            Func<bool> hasDataToRead = () => _stateKeeper.HasDataToRead;
            Func<bool> hasDataToWrite = () => _stateKeeper.HasDataToWrite;
            Func<bool> hasDataToProcess = () => _stateKeeper.BlocksToProcessCount > 0;

            IDecisionNode read = new Decision("Read", () => _blockProcessor.ReadBlock());
            IDecisionNode process = new Decision("Process", () => _blockProcessor.ProcessBlock());
            IDecisionNode write = new Decision("Write", () => _blockProcessor.WriteBlock());
            IDecisionNode wait = new Decision("Wait", () => { });
            IDecisionNode finish = new Decision("Finish", () => { });

            IDecisionNode isEverythingReadedNode = new DecisionCondition("isEverythingReaded", () => !hasDataToRead(), finish, wait);
            IDecisionNode hasDataToProcessNode = new DecisionCondition("hasDataToProcess", hasDataToProcess, process, isEverythingReadedNode);
            IDecisionNode hasDataToWriteNode = new DecisionCondition("hasDataToWrite", hasDataToWrite, write, hasDataToProcessNode);
            IDecisionNode isWritingInProcessNode = new DecisionCondition("isWritingInProcess", isWritingInProcess, hasDataToProcessNode, hasDataToWriteNode);
            IDecisionNode hasDataToReadNode = new DecisionCondition("hasDataToRead", hasDataToRead, read, isWritingInProcessNode);
            IDecisionNode isReadInProcessNode = new DecisionCondition("isReadInProcess", isReadInProcess, hasDataToProcessNode, hasDataToReadNode);
            IDecisionNode isEnoughMemoryNode = new DecisionCondition("isEnoughMemoryNode", isEnoughMemory, isReadInProcessNode, isWritingInProcessNode);

            return isEnoughMemoryNode;
        }

        private IDecisionNode BuildDecisionGraphForDecompression()
        {
            Func<bool> isEnoughMemory = () => _stateKeeper.IsEnoughMemory;
            Func<bool> isReadInProcess = () => _stateKeeper.IsReadInProcess;
            Func<bool> isWritingInProcess = () => _stateKeeper.IsWriteInProcess;

            Func<bool> hasDataToRead = () => _stateKeeper.HasDataToRead;
            Func<bool> hasDataToWrite = () => _stateKeeper.HasDataToWrite;
            Func<bool> hasDataToProcess = () => _stateKeeper.BlocksToProcessCount > 0;

            IDecisionNode read = new Decision("Read", () => _blockProcessor.ReadBlock());
            IDecisionNode process = new Decision("Process", () => _blockProcessor.ProcessBlock());
            IDecisionNode write = new Decision("Write", () => _blockProcessor.WriteBlock());
            IDecisionNode wait = new Decision("Wait", () => { });
            IDecisionNode finish = new Decision("Finish", () => { });
            IDecisionNode callGCAndWait = new Decision("Write", () =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            });

            IDecisionNode isEverythingReadedNode = new DecisionCondition(
                "isEverythingReaded", 
                () => !hasDataToRead(), 
                finish, 
                wait);

            IDecisionNode hasDataToProcessNode = new DecisionCondition(
                "hasDataToProcess", 
                hasDataToProcess, 
                process, 
                isEverythingReadedNode);

            IDecisionNode hasDataToWriteEnoughMemoryNode = new DecisionCondition(
                "hasDataToWriteEnoughMemoryNode", 
                hasDataToWrite, 
                write, 
                hasDataToProcessNode);

            IDecisionNode hasDataToWriteNode = new DecisionCondition(
                "hasDataToWrite", 
                hasDataToWrite, 
                write, 
                callGCAndWait);

            IDecisionNode isWritingInProcessEnoughMemoryNode = new DecisionCondition(
                "isWritingInProcessEnoughMemoryNode", 
                isWritingInProcess, 
                hasDataToProcessNode, 
                hasDataToWriteEnoughMemoryNode);

            IDecisionNode isWritingInProcessNode = new DecisionCondition(
                "isWritingInProcess", 
                isWritingInProcess, 
                wait, 
                hasDataToWriteNode);

            IDecisionNode hasDataToReadNode = new DecisionCondition(
                "hasDataToRead", 
                hasDataToRead, 
                read, 
                isWritingInProcessEnoughMemoryNode);

            IDecisionNode isReadInProcessNode = new DecisionCondition(
                "isReadInProcess", 
                isReadInProcess, 
                hasDataToProcessNode, 
                hasDataToReadNode);

            IDecisionNode isEnoughMemoryNode = new DecisionCondition(
                "isEnoughMemoryNode", 
                isEnoughMemory, 
                isReadInProcessNode, 
                isWritingInProcessNode);

            return isEnoughMemoryNode;
        }
    }
}
