using System;

namespace MultithreadedGZip.BLL.MultithreadedExtensions
{
    public abstract class MultithreadedGZipExecutor
    {
        public MultithreadedGZipExecutor(string inputFilePath, string outputFilePath)
        {
            ThrowIsNullOrWhiteSpace(inputFilePath, out this.inputFilePath);
            ThrowIsNullOrWhiteSpace(outputFilePath, out this.outputFilePath);
        }

        private void ThrowIsNullOrWhiteSpace(string inStr, out string outStr)
        {
            if (string.IsNullOrWhiteSpace(inStr))
                throw new ArgumentNullException(inStr);
            outStr = inStr;
        }

        protected readonly string inputFilePath;
        protected readonly string outputFilePath;

        public abstract void Execute();
        public abstract void Wait();
    }
}
