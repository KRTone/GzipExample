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
        readonly ICompressor compressor;
        readonly IQueuedThreadPool threadPool;
        event BlockHandler BlockReaded;
        event Action StreamEnded;

        Stream inStream;
        Stream newStream;
        GZipStream gzipStream;

        public GZipExecutor(IMultithreadedConfigurator multithreadedConfigurator,
            IGZipConfigurator gZipConfigurator,
            ICustomSemaphore semaphore,
            IBlocksEngine blocksEngine,
            ILogService logger,
            ICompressor compressor,
            IQueuedThreadPool threadPool)
        {
            this.threadPool = threadPool;
            this.blocksEngine = blocksEngine ?? throw new ArgumentNullException(nameof(GZipExecutor.blocksEngine));
            this.multithreadedConfigurator = multithreadedConfigurator ?? throw new ArgumentNullException(nameof(multithreadedConfigurator));
            this.gZipConfigurator = gZipConfigurator ?? throw new ArgumentNullException(nameof(gZipConfigurator));
            this.semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.compressor = compressor ?? throw new ArgumentNullException(nameof(compressor));
            //InitializeStreams();
        }

        public void Execute()
        {
            int blockNumber = 0;

            while (true)
            {
                var buffer = new byte[multithreadedConfigurator.BlockSize];
                semaphore.WaitOne();
                int size = compressor.ReadBytes(buffer, multithreadedConfigurator.BlockSize);

                if (size == 0)
                {
                    semaphore.Release();
                    StreamEnded?.Invoke();
                    blocksEngine.Awaiter.WaitOne();
                    return;
                }

                var block = new Block(blockNumber++, buffer, size);
                threadPool.AddActionToQueue(() => compressor.WriteBlock(block));
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
