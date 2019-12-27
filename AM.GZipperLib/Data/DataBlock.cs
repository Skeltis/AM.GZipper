using System;

namespace AM.GZipperLib.Data
{
    internal class DataBlock : IBlock
    {
        public Int32 BlockNumber { get; }

        public byte[] Data { get; private set; }

        public int Length => Data.Length;

        public DataBlock(byte[] data, Int32 blockNumber)
        {
            BlockNumber = blockNumber;
            Data = data;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }
                Data = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
