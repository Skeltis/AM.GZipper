using System;
using System.IO;
using AM.GZipperLib.Data;

namespace AM.GZipperLib
{

    internal class SequentialBlockWriter : ISequentialWriter
    {
        private object sync = new object();
        private FileStream _fileStream;

        private Int64 _position;
        private Int32 _currentBlock;
        private Int32 _lastBlockSize;

        public SequentialBlockWriter(string fileName)
        {
            _fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
            _fileStream.SetLength(0);
            Reset();
        }

        public SequentialBlockWriter(string fileName, Int64 fileSize) : this(fileName)
        {
            _fileStream.SetLength(fileSize);
        }

        public void Reset()
        {
            lock (sync)
            {
                _position = -1;
                _currentBlock = 0;
            }
        }

        public void MoveNext()
        {
            lock (sync)
            {
                if (_position == -1)
                {
                    _position = 0;
                }
                else
                {
                    _position += _lastBlockSize;
                    _currentBlock++;
                }
                _fileStream.Seek(_position, SeekOrigin.Begin);
            }
        }

        public void WriteBlock(IBlock block)
        {
            lock (sync)
            {
                if (_position == -1) throw new Exception();

                _lastBlockSize = block.Length;
                _fileStream.Write(block.Data, 0, block.Length);
                _fileStream.Flush(true);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
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
