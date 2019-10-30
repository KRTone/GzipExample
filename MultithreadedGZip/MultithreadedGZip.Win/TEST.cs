using BaltBet.CashDevices.Atol;
using BaltBet.CashDevices.Atol.Common;
using CommonServiceLocator;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Tests.KS;

namespace Tests
{
    class Program
    {
        public static readonly BetType[] betTypes = new BetType[4] { BetType.Live, BetType.Normal, BetType.SuperExpress, BetType.System };
        public static readonly PlayerType[] playerTypes = new PlayerType[1] { PlayerType.KrmAndPps };

        static void Main()
        {
        }

        public class Block
        {
            public Block(int number)
            {
                Number = number;
                Bytes = new byte[Size];
            }
            public int Number { get; }
            public byte[] Bytes { get; }
            public int Size => 1024 * 1024;
        }

        public class QueuedThreadPool
        {
            public QueuedThreadPool()
            {
                DynamicSemaphore writeLimiter = new DynamicSemaphore(1);
                DynamicSemaphore readLimiter = new DynamicSemaphore(1);

                threads = new List<Thread>();
                int processorCount = Environment.ProcessorCount - 1;
                for (int i = 0; i < processorCount; ++i)
                {
                    var thread = new Thread(ExecuteRead) { IsBackground = true };
                    thread.Start();
                    threads.Add(thread);
                }
                blocksToWrite = new List<Block>(processorCount);
            }

            readonly object _lockRead = new object();
            readonly object _lockWrite = new object();
            readonly List<Thread> threads;
            readonly List<Block> blocksToWrite;
            readonly DynamicSemaphore writeLimiter;
            readonly DynamicSemaphore readLimiter;
            int expectedBlockNum = 0;

            void ExecuteRead()
            {
                while (true)
                {
                    lock (_lockRead)
                    {
                        while (!blocksToWrite.Contains(null))
                            Monitor.Wait(_lockRead);
                        //read

                    }
                }
            }

            void ExecuteWrite()
            {
                while (true)
                {
                    while (blocksToWrite.FirstOrDefault(f => f.Number != expectedBlockNum) == null)
                        Monitor.Wait(_lockRead);

                    //write [expecedBlockNum]
                    expectedBlockNum++;
                    blocksToWrite.Remove(blocksToWrite.First(w => w.Number == expectedBlockNum));
                    Monitor.Pulse(_lockRead);
                }
            }
        }

        public sealed class DynamicSemaphore
        {
            public DynamicSemaphore(int count)
            {
                if (count < 1)
                    throw new ArgumentException(nameof(maxCount));

                maxCount = CurrentCount = count;
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
}