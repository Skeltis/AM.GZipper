using System;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Threading;
using AM.GZipperLib.Data;

namespace AM.GZipperLib
{
    internal class SequentialBlockProcessor : IBlockProcessor
    {
        private CompressionConfig _config;

        private SharedProcessState _state;
        private ISequentialReader _blockReader;
        private ISequentialWriter _blockWriter;
        private IBlockCompresser _compresser;
        private BlockScribe _scriber;
        private IProgressInformer _progressInformer;
        private CancellationToken _cancellationToken;

        private ConcurrentQueue<IBlock> _inputBlocks;
        private ConcurrentDictionary<int, IBlock> _outputBlocks;

        public SequentialBlockProcessor(CompressionConfig config, 
                                        BlockScribe scribe, 
                                        SharedProcessState stateKeeper, 
                                        CancellationToken cancellationToken)
        {
            _scriber = scribe;
            _config = config;
            _state = stateKeeper;
            _cancellationToken = cancellationToken;

            _compresser = new GZipBlockCompresser(config.Level);
            _inputBlocks = new ConcurrentQueue<IBlock>();
            _outputBlocks = new ConcurrentDictionary<int, IBlock>();

            InitWriter();
            InitReader();
        }

        public void SetProgressInformer(IProgressInformer informer)
        {
            _progressInformer = informer;
            if (_progressInformer != null) _progressInformer.SetOverallValue(_scriber.TotalBlocks * 3);
        }

        public void ReadBlock()
        {
            IBlock block = null;
            try
            {
                block = _blockReader.ReadBlock();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            _inputBlocks.Enqueue(block);
            bool result = _blockReader.MoveNext();
            _state.InformBlockReaded();
            _progressInformer?.IncrementValue();
        }

        public void ProcessBlock()
        {
            bool success = _inputBlocks.TryDequeue(out IBlock toProcess);
            if (!success)
            {
                _state.ReleaseBlockForProcessing();
                return;
            }

            IBlock result = null;
            try
            {
                result = _config.Mode == CompressionMode.Compress ? 
                    _compresser.Compress(toProcess) : 
                    _compresser.Decompress(toProcess);
            }
            catch (Exception ex)
            {
                _inputBlocks.Enqueue(toProcess);
                _state.ReleaseBlockForProcessing();
                throw ex;
            }

            _outputBlocks.TryAdd(result.BlockNumber, result);
            _state.InformBlockProcessed(toProcess.BlockNumber);
            toProcess.Dispose();
            _progressInformer?.IncrementValue();
        }

        public void WriteBlock()
        {
            while (!_cancellationToken.IsCancellationRequested && _state.HasDataToWrite)
            {
                SingleBlockWrite();
            }
        }

        private void SingleBlockWrite()
        {
            Int32 currentBlock = _state.LastWrittenBlock + 1;
            IBlock toProcess = null;
            
            bool result = _outputBlocks.TryRemove(currentBlock, out toProcess);
            if (!result) return;
            try
            {
                if (_config.Mode == CompressionMode.Compress)
                {
                    IBlock tmpBlock = _scriber.ScribeBlock(toProcess);
                    toProcess.Dispose();
                    toProcess = tmpBlock;
                }
                _blockWriter.WriteBlock(toProcess);
            }
            catch (Exception ex)
            {
                result = _outputBlocks.TryAdd(toProcess.BlockNumber, toProcess);
                throw ex;
            }
            _state.InformBlockWritten();
            toProcess.Dispose();
            _blockWriter.MoveNext();
            _progressInformer?.IncrementValue();
        }


        private void InitReader()
        {
            _blockReader = (_config.Mode == CompressionMode.Compress) ?
                new SequentialDataBlockReader(_config.InputFile, _config.BlockSize, 0, 0) as ISequentialReader :
                new SequentialGzipBlockReader(_config.InputFile) as ISequentialReader;

            bool result = _blockReader.MoveNext();
        }

        private void InitWriter()
        {
            _blockWriter = (_config.Mode == CompressionMode.Compress) ?
                new SequentialBlockWriter(_config.OutputFile) :
                new SequentialBlockWriter(_config.OutputFile, _scriber.OriginalFileSize);

            _blockWriter.MoveNext();
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
                    _blockReader.Dispose();
                    _blockWriter.Dispose();
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
