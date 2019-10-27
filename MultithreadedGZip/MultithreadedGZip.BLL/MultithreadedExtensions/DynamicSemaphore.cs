using MultithreadedGZip.BLL.Interfaces;
using MultithreadedGZip.BLL.Interfaces.Configurators;
using System;
using System.Threading;

namespace MultithreadedGZip.BLL.MultithreadedExtensions
{
    public sealed class DynamicSemaphore : ICustomSemaphore
    {
        public DynamicSemaphore(IMultithreadedConfigurator configurator)
        {
            if (configurator.Processors < 1)
                throw new ArgumentException(nameof(maxCount));

            maxCount = CurrentCount = configurator.Processors;
        }

        private object _lock = new object();
        private int maxCount;
        public int CurrentCount { get; private set; }

        public void WaitOne()
        {
            lock (_lock)
            {
                while (CurrentCount <= 0)
                {
                    Monitor.Wait(_lock);
                }
                CurrentCount--;
            }
        }

        public void Release()
        {
            lock (_lock)
            {
                if (CurrentCount < maxCount)
                {
                    CurrentCount++;
                    Monitor.Pulse(_lock);
                }
                else
                    throw new SemaphoreFullException("Semaphore released too many times.");
            }
        }
    }
}
