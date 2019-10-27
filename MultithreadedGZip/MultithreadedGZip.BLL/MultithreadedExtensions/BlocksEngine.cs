using MultithreadedGZip.BLL.Common;
using MultithreadedGZip.BLL.Interfaces;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace MultithreadedGZip.BLL.MultithreadedExtensions
{
    public delegate void BlockHandler(Block block, Stream outStream);

    public class BlocksEngine : IBlocksEngine
    {
        readonly ICustomSemaphore semaphore;
        readonly IBlockWriter writer;
        readonly IQueuedThreadPool threadPool;
        readonly int counterStartValue;
        readonly ILogService logger;
        int noBlocksExpected;
        public ManualResetEvent Awaiter { get; }

        public BlocksEngine(ICustomSemaphore semaphore, 
            IBlockWriter writer,
            IQueuedThreadPool threadPool,
            ILogService logger)
        {
            this.threadPool = threadPool ?? throw new ArgumentNullException(nameof(threadPool));
            this.semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            counterStartValue = semaphore.CurrentCount;
            Awaiter = new ManualResetEvent(false);
        }

        void HandleBlockInternal(Block block, Stream outStream)
        {
            logger.Info($"BlocksEngine.HandleBlockInternal(blockNum = {block.Number})");
            writer.Write(block, outStream);
            semaphore.Release();
            UpdateAwaiter();
        }

        void UpdateAwaiter()
        {
            Thread.MemoryBarrier();
            if (Thread.VolatileRead(ref noBlocksExpected) != 0)
                if (counterStartValue == semaphore.CurrentCount)
                    Awaiter.Set();
        }

        public void HandleBlock(Block block, Stream outStream)
        {
            threadPool.AddActionToQueue(() => HandleBlockInternal(block, outStream));
        }

        public void EndOfBlocks()
        {
            Interlocked.Exchange(ref noBlocksExpected, 1);
            UpdateAwaiter();
            logger.Info("BlocksEngine.EndOfBlocks()");
        }
    }
}
