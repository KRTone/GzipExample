using MultithreadedGZip.BLL.Common;
using System;
using System.IO;
using System.Threading;

namespace MultithreadedGZip.BLL.Interfaces
{
    public interface IThreadsManagers
    {
        void AddAction(Action action);
        void End();
        ManualResetEvent Awaiter { get; }
    }
}
