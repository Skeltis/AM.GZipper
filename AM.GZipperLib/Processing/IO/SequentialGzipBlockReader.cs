using System;
using System.IO;
using AM.GZipperLib.DataFormat;
using AM.GZipperLib.Data;

namespace AM.GZipperLib
{
    internal class SequentialGzipBlockReader : ISequentialReader
    {
        private object sync = new object();
        private FileStream _fileStream;
        private Int32 _currentBlock;
        private Int64 _position;
        private Int32 _currentSize;

        private bool _lastBlock = false;


        public SequentialGzipBlockReader(string fileName)
        {
            _fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            Reset();
        }

        public void Reset()
        {
            lock (sync)
            {
                _position = -1;
                _currentBlock = 0;
                _lastBlock = false;
            }
        }

        public bool MoveNext()
        {
            lock (sync)
            {
                if (_lastBlock) return false;

                _position = (_position == -1) ? 0 : _position + _currentSize;

                var currInfo = new GzipFileInfo(_fileStream, _position);

                var blockInfo = currInfo.FindFirstFieldOrDefault(BlockInfoField.SI1_BlockNumber, BlockInfoField.SI2_BlockNumber) as BlockInfoField;
                if (blockInfo == null) throw new FormatException("Inconsistent file format");
                _currentSize = (Int32)(blockInfo.DataSize + currInfo.HeaderSize + GzipFileInfo.TAIL_SIZE);

                if (_position + _currentSize >= _fileStream.Length)
                {
                    _lastBlock = true;
                }
                
                _currentBlock = (Int32)blockInfo.BlockNumber;
                _fileStream.Seek(_position, SeekOrigin.Begin);
                return true;
            }
        }

        public IBlock ReadBlock()
        {
            lock (sync)
            {
                if (_position == -1) throw new Exception();

                var readedBlock = new byte[_currentSize];
                _fileStream.Read(readedBlock, 0, _currentSize);

                IBlock block = new DataBlock(readedBlock, _currentBlock);
                _fileStream.Flush(true);

                return block;
            }
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
                    _fileStream.Dispose();
                }
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
