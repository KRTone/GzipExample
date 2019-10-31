using System;
using System.Threading;

namespace MultithreadedGZip.BLL.MultithreadedExtensions
{
    public abstract class MultithreadedGZipExecutor
    {
        public MultithreadedGZipExecutor(string inputFilePath, string outputFilePath)
        {
            ThrowIsNullOrWhiteSpace(inputFilePath, out this.inputFilePath);
            ThrowIsNullOrWhiteSpace(outputFilePath, out this.outputFilePath);
            executeThread = new Thread(Execution) { IsBackground = true };
            resetEvent = new ManualResetEvent(false);
        }

        private void ThrowIsNullOrWhiteSpace(string inStr, out string outStr)
        {
            if (string.IsNullOrWhiteSpace(inStr))
                throw new ArgumentNullException(inStr);
            outStr = inStr;
        }

        protected readonly string inputFilePath;
        protected readonly string outputFilePath;
        protected readonly Thread executeThread;
        readonly ManualResetEvent resetEvent;

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

        private void Execution()
        {
            InternalExecute();
            resetEvent.Set();
        }
    }
}
