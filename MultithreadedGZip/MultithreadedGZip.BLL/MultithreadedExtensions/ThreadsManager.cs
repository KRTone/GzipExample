using MultithreadedGZip.BLL.Common;
using MultithreadedGZip.BLL.Interfaces;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace MultithreadedGZip.BLL.MultithreadedExtensions
{
    public class ThreadsManager : IThreadsManagers
    {
        readonly ICustomSemaphore semaphore;
        readonly IBlockWriter writer;
        readonly IQueuedThreadPool threadPool;
        readonly int counterStartValue;
        readonly ILogService logger;
        int noBlocksExpected;
        public ManualResetEvent Awaiter { get; }
        int counter = 0;

        public ThreadsManager(ICustomSemaphore semaphore, 
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

        void UpdateAwaiter()
        {
            Thread.MemoryBarrier();
            if (Thread.VolatileRead(ref noBlocksExpected) != 0)
                if (counterStartValue == semaphore.CurrentCount)
                    Awaiter.Set();
        }

        public void AddAction(Action readAction)
        {
            threadPool.AddActionToQueue(() =>
            {
                readAction.Invoke();
                semaphore.Release();
                UpdateAwaiter();
            });
        }

        public void End()
        {
            Interlocked.Exchange(ref noBlocksExpected, 1);
            UpdateAwaiter();
            logger.Info("BlocksEngine.EndOfBlocks()");
        }
    }
}
