using System;

namespace MultithreadedGZip.BLL.Interfaces
{
    public interface IGZipExecutor : IDisposable
    {
        void Execute();
    }
}
