using System;
using System.Collections.Generic;
using System.Threading;
using AM.GZipperLib.DecisionTree;

namespace AM.GZipperLib
{
    internal sealed class BlockTaskDispatcher : IBlockTaskDispatcher
    {
        private Int32 _maxTaskCount;

        private object sync = new object();
        private CountdownEvent cde;
        private ManualResetEventSlim continueEvent = new ManualResetEventSlim(true);

        private IBlockProcessor _blockProcessor;
        private SharedProcessState _stateKeeper;
        private IDecisionNode _decisionGraph;

        private List<BlockTask> _blockTaskPool;
        private CancellationTokenSource _tokenSource;
       

        public BlockTaskDispatcher(IBlockProcessor processor, 
                                   IDecisionNode strategy, 
                                   SharedProcessState stateKeeper,
                                   CancellationTokenSource cancellationTokenSource,
                                   Int32 maxTaskCount)
        {
            _maxTaskCount = maxTaskCount;
            _stateKeeper = stateKeeper;
            _blockProcessor = processor;
            _decisionGraph = strategy;
            _tokenSource = cancellationTokenSource;

            _blockTaskPool = new List<BlockTask>(_maxTaskCount);
            for (int i = 0; i < _maxTaskCount; i++)
            {
                _blockTaskPool.Add(new BlockTask(this));
            }
        }

        public void RunAndWait()
        {
            if (cde == null || cde.IsSet)
            {
                cde = new CountdownEvent(_maxTaskCount);
                foreach (var task in _blockTaskPool)
                {
                    task.RunThread(() => WorkCycle(task, _tokenSource.Token));
                }
            }
            WaitCompletion();
        }

        public void Cancel()
        {
            _tokenSource.Cancel();
            continueEvent.Set();
        }

        private void WaitCompletion()
        {
            cde.Wait();
            _blockProcessor.Dispose();
            foreach (var task in _blockTaskPool)
            {
                task.TryRethrowException();
            }
        }

        private void WorkCycle(BlockTask task, CancellationToken token)
        {
            while (!token.IsCancellationRequested && task.TaskType != BlockTaskType.Finish)
            {
                Tuple<Decision, BlockTaskType> plannedTask = AssignDecision();
                Action plannedWork = plannedTask.Item1.GetResolution();
                task.PerformAction(plannedWork, plannedTask.Item2);
                if (task.TaskType == BlockTaskType.Wait)
                {
                    continueEvent.Reset();
                    continueEvent.Wait();
                }
                InformCompletion(task);
            }
            cde.Signal();
        }


        private Tuple<Decision, BlockTaskType> AssignDecision()
        {
            BlockTaskType type;
            Decision decision = null;
            lock (sync)
            {
                decision = _decisionGraph.FindDecision();
                type = (BlockTaskType)Enum.Parse(typeof(BlockTaskType), decision.Name);
                SetStatesOnExecution(type);
            }
            return Tuple.Create(decision, type);
        }

        private void SetStatesOnExecution(BlockTaskType type)
        {
            if (type == BlockTaskType.Read)
            {
                _stateKeeper.InformReadInProcess();
            }
            else if (type == BlockTaskType.Write)
            {
                _stateKeeper.InformWriteInProcess();
            }
            else if (type == BlockTaskType.Process)
            {
                _stateKeeper.ReserveBlockForProcessing();
            }
        }

        private void InformCompletion(BlockTask task)
        {
            lock (sync)
            {
                SetStatesOnCompletion(task.TaskType);
                if (task.WasErrored) Cancel();
            }

            continueEvent.Set();
        }

        private void SetStatesOnCompletion(BlockTaskType type)
        {
            if (type == BlockTaskType.Read)
            {
                _stateKeeper.InformReadCompleted();
            }
            else if (type == BlockTaskType.Write)
            {
                _stateKeeper.InformWriteCompleted();
            }
        }

    }
}
