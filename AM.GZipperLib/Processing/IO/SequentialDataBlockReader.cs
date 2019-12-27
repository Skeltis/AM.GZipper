using System;
using System.IO;
using AM.GZipperLib.Data;

namespace AM.GZipperLib
{
    internal class SequentialDataBlockReader : ISequentialReader
    {
        private object sync = new object();
        private FileStream _fileStream;
        private Int32 _currentBlock;
        private Int64 _position;
        private Int32 _blockSize;
        private Int32 _currentSize;

        private Int32 _startOffset;
        private Int32 _endOffset;

        private Int64 dataLeft => _fileStream.Length - _endOffset - _position;

        public SequentialDataBlockReader(string fileName, Int32 blockSize, Int32 startOffset, Int32 endOffset)
        {
            _fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            _blockSize = blockSize;
            _startOffset = startOffset;
            _endOffset = endOffset;

            Reset();
        }

        public void Reset()
        {
            lock (sync)
            {
                _position = -1;
                _currentBlock = 0;
            }
        }

        public bool MoveNext()
        {
            lock (sync)
            {
                if (_position == -1)
                {
                    _position = _startOffset;
                    _currentBlock = 1;
                }
                else if (dataLeft <= _blockSize)
                {
                    return false;
                }
                else
                {
                    _currentBlock++;
                    _position += _blockSize;
                }

                _fileStream.Seek(_position, SeekOrigin.Begin);
                _currentSize = dataLeft >= _blockSize ? _blockSize : (Int32)dataLeft;
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
