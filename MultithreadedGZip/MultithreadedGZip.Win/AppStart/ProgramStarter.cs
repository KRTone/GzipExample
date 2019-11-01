using MultithreadedGZip.BLL.Interfaces;
using System;

namespace MultithreadedGZip.Win.AppStart
{
    public class ProgramStarter
    {
        public ProgramStarter(IMultithreadedGZipExecutor zipper)
        {
            this.zipper = zipper ?? throw new ArgumentNullException(nameof(zipper));
        }

        readonly IMultithreadedGZipExecutor zipper;

        public void Run()
        {
            zipper.Execute(true);
        }
    }
}