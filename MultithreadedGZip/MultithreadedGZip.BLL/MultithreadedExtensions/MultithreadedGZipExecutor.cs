using MultithreadedGZip.BLL.Common;
using MultithreadedGZip.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MultithreadedGZip.BLL.MultithreadedExtensions
{
    public abstract class MultithreadedGZipExecutor : IMultithreadedGZipExecutor
    {
        public MultithreadedGZipExecutor(string inputFilePath, string outputFilePath, int blockSize, int processors, ILogService logService)
        {
            this.logService = logService ?? throw new ArgumentNullException(nameof(logService));
            ThrowIsNullOrWhiteSpace(inputFilePath, out this.inputFilePath);
            ThrowIsNullOrWhiteSpace(outputFilePath, out this.outputFilePath);
            executeThread = new Thread(Execution) { IsBackground = true };
            resetEvent = new ManualResetEvent(false);

            //один поток на запись на протяжении всего цикла
            //остальные заняты (архи/разархи)винрованием
            blocksToWriteCount = processors - 1;
            readThreadPool = new QueuedThreadPool(blocksToWriteCount);
            readThreadPool.OnException += ThrowOnException;
            blocksToWrite = new List<Block>();
            this.blockSize = blockSize;
        }

        protected readonly string inputFilePath;
        protected readonly string outputFilePath;
        protected readonly Thread executeThread;
        readonly ManualResetEvent resetEvent;

        protected readonly QueuedThreadPool readThreadPool;
        protected readonly int blocksToWriteCount;
        protected readonly List<Block> blocksToWrite;
        protected readonly int blockSize;
        protected readonly object _lockWrite = new object();

        protected readonly ILogService logService;

        private void ThrowIsNullOrWhiteSpace(string inStr, out string outStr)
        {
            if (string.IsNullOrWhiteSpace(inStr))
                throw new ArgumentNullException(inStr);
            outStr = inStr;
        }

        public virtual void Execute(bool wait)
        {
            executeThread.Start();
            if (wait)
                resetEvent.WaitOne();
        }
        protected abstract void InternalExecute();
        public virtual void Wait()
        {
            resetEvent.WaitOne();
        }

        void Execution()
        {
            logService.Info($"Start {this.GetType().Name}");
            InternalExecute();
            logService.Info($"Completed {this.GetType().Name}");
            resetEvent.Set();
        }

        void ThrowOnException(Exception ex)
        {
            executeThread.Abort();
            logService.Info($"Stopped {this.GetType().Name}");
            logService.Exception(ex);
            throw ex;
        }
    }
}
