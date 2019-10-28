using MultithreadedGZip.BLL.Common;
using MultithreadedGZip.BLL.Interfaces;
using MultithreadedGZip.BLL.Interfaces.Configurators;
using MultithreadedGZip.BLL.MultithreadedExtensions;
using System;
using System.IO;
using System.IO.Compression;

namespace MultithreadedGZip.BLL.GZip
{
    public class GZipExecutor : IGZipExecutor
    {
        readonly ICustomSemaphore semaphore;
        readonly IMultithreadedConfigurator multithreadedConfigurator;
        readonly IGZipConfigurator gZipConfigurator;
        readonly IBlocksEngine blocksEngine;
        readonly ILogService logger;
        event BlockHandler BlockReaded;
        event Action StreamEnded;

        Stream inStream;
        Stream newStream;
        GZipStream gzipStream;

        public GZipExecutor(IMultithreadedConfigurator multithreadedConfigurator,
            IGZipConfigurator gZipConfigurator,
            ICustomSemaphore semaphore,
            IBlocksEngine blocksEngine,
            ILogService logger)
        {
            this.blocksEngine = blocksEngine ?? throw new ArgumentNullException(nameof(GZipExecutor.blocksEngine));
            this.multithreadedConfigurator = multithreadedConfigurator ?? throw new ArgumentNullException(nameof(multithreadedConfigurator));
            this.gZipConfigurator = gZipConfigurator ?? throw new ArgumentNullException(nameof(gZipConfigurator));
            this.semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeStreams();
            BlockReaded += blocksEngine.HandleBlock;
            StreamEnded += blocksEngine.EndOfBlocks;
        }

        public void Execute()
        {
            int blockNumber = 0;

            while (true)
            {
                semaphore.WaitOne();
                var buffer = new byte[multithreadedConfigurator.BlockSize];
                int size = 0;
                bool isCompress = gZipConfigurator.CompressionMode == CompressionMode.Compress;
                if (isCompress)
                {
                    size = inStream.Read(buffer, 0, multithreadedConfigurator.BlockSize);
                }
                else
                {
                    size = gzipStream.Read(buffer, 0, multithreadedConfigurator.BlockSize);
                }
                if (size == 0)
                {
                    semaphore.Release();
                    StreamEnded?.Invoke();
                    blocksEngine.Awaiter.WaitOne();
                    return;
                }
                var block = new Block(blockNumber++, buffer, size);
                if (isCompress)
                {
                    BlockReaded?.Invoke(block, gzipStream);
                }
                else
                {
                    BlockReaded?.Invoke(block, newStream);
                }
            }
        }

        void InitializeStreams()
        {
            logger.Info("Start GZipExecutor.InitializeStreams()");

            inStream = File.OpenRead(gZipConfigurator.InFilePath);
            newStream = new FileStream(gZipConfigurator.OutFilePath, FileMode.Create, FileAccess.ReadWrite);

            if (gZipConfigurator.CompressionMode == CompressionMode.Compress)
            {
                gzipStream = new GZipStream(newStream, CompressionMode.Compress);
            }
            else if (gZipConfigurator.CompressionMode == CompressionMode.Decompress)
            {
                gzipStream = new GZipStream(inStream, CompressionMode.Decompress);
            }
            else
            {
                var ex = new ArgumentException(nameof(gZipConfigurator.CompressionMode));
                logger.Exception(ex);
                throw ex;
            }

            logger.Info("Complete GZipExecutor.InitializeStreams()");
        }

        bool isDisposed = false;

        public void Dispose()
        {
            if (!isDisposed)
            {
                logger.Info("Start GZipExecutor.Dispose()");
                gzipStream.Dispose();
                newStream.Dispose();
                inStream.Dispose();
                logger.Info("Complete GZipExecutor.Dispose()");
                isDisposed = true;
            }
        }
    }
}
