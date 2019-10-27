using System;

namespace MultithreadedGZip.BLL.Interfaces
{
    public interface IQueuedThreadPool
    {
        void AddActionToQueue(Action action);
    }
}
