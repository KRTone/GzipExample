using MultithreadedGZip.BLL.Interfaces;
using System;

namespace MultithreadedGZip.Win.AppStart
{
    public class ProgramStarter : IDisposable
    {
        public ProgramStarter(IGZipExecutor zipper)
        {
            this.zipper = zipper ?? throw new ArgumentNullException(nameof(zipper));
        }

        readonly IGZipExecutor zipper;

        public void Run()
        {
            zipper.Execute();
        }

        bool isDisposed = false;

        public void Dispose()
        {
            if (!isDisposed)
            {
                zipper.Dispose();
                isDisposed = true;
            }
        }
    }
}