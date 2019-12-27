using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AM.GZipperLib;

namespace GZipper
{
    public class ConsoleInformer : IProgressInformer
    {
        private object writeLock = new object();

        private Int64 _currentValue = 0;
        private Int64 _totalValue = 1;

        public void IncrementValue()
        {
            Interlocked.Increment(ref _currentValue);
            lock (writeLock)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write("Progress: {0,7:0.00}%", ((double)_currentValue / _totalValue) * 100);
            }
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _currentValue, 0);
        }

        public void SetOverallValue(Int64 value)
        {
            Interlocked.Exchange(ref _totalValue, value);
        }
    }
}
