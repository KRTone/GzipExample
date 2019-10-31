using System;
using System.Collections.Generic;
using System.Threading;

namespace MultithreadedGZip.BLL.MultithreadedExtensions
{
    public class QueuedThreadPool
    {
        public QueuedThreadPool(int count)
        {
            actionQueue = new Queue<Action>();
            threads = new List<Thread>();
            for (int i = 0; i < count; ++i)
            {
                var thread = new Thread(Execute) { IsBackground = true };
                thread.Start();
                threads.Add(thread);
            }
        }

        readonly Queue<Action> actionQueue;
        readonly List<Thread> threads;

        void Execute()
        {
            while (true)
            {
                Action action;

                lock (actionQueue)
                {
                    while (actionQueue.Count == 0)
                        Monitor.Wait(actionQueue);
                    action = actionQueue.Dequeue();
                }
                action();
            }
        }

        public void AddActionToQueue(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            lock (actionQueue)
            {
                actionQueue.Enqueue(action);
                Monitor.Pulse(actionQueue);
            }
        }
    }
}
