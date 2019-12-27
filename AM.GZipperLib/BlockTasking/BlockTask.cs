using System;
using System.Threading;

namespace AM.GZipperLib
{
    internal enum BlockTaskType { Wait, Read, Process, Write, Finish }
    internal enum BlockTaskState { Awaiting = 0, Running = 1 }

    internal sealed class BlockTask
    {
        private BlockTaskDispatcher _pool;
        private Int32 _taskState;
        private Thread _thread;
        private Exception _exception;

        public BlockTaskType TaskType { get; private set; }

        public bool WasErrored => _exception != null;

        public BlockTaskState State => (BlockTaskState)_taskState;

        public BlockTask(BlockTaskDispatcher pool)
        {
            _pool = pool;
            _taskState = 0;
        }

        public void RunThread(Action action)
        {
            _thread = new Thread(() => action());
            _thread.Start();
        }

        public void PerformAction(Action action, BlockTaskType taskType)
        {
            TaskType = taskType;
            Interlocked.Exchange(ref _taskState, 1);
            try
            {
                action.Invoke();
            }
            catch (Exception exception)
            {
                _exception = exception;
            }
            Interlocked.Exchange(ref _taskState, 0);
        }

        public void TryRethrowException()
        {
            if (_exception != null)
                throw _exception;
        }
    }
}
