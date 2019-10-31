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

namespace Tests
{
    class Program
    {
        static void Main()
        {
            var ex = new MultithreadedReadWriteEngine();
            ex.Execute();
            ex.Wait();
        }

        public static void Decompress()
        {
            using (FileStream originalFileStream = File.OpenRead(@"C:\Backups\4k.jpg.gz"))
            {
                using (FileStream decompressedFileStream = File.Create(@"C:\Backups\4k2.jpg"))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }
            }
        }

        public class Block
        {
            public Block(int number, int size)
            {
                Number = number;
                Size = size;
            }
            public int Number { get; }
            public byte[] Data { get; set; }
            public int Size { get; }
            public bool Readed { get; set; }
        }

        public sealed class DynamicSemaphore
        {
            public DynamicSemaphore(int maxCount)
            {
                if (maxCount < 1)
                    throw new ArgumentException(nameof(maxCount));

                this.maxCount = CurrentCount = maxCount;
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

        public class QueuedThreadPool
        {
            public QueuedThreadPool(int count)
            {
                actionQueue = new Queue<Action>();
                threads = new List<Thread>();
                dynamicSemaphore = new DynamicSemaphore(count);
                for (int i = 0; i < count; ++i)
                {
                    var thread = new Thread(Execute) { IsBackground = true };
                    thread.Start();
                    threads.Add(thread);
                }
            }

            readonly Queue<Action> actionQueue;
            readonly List<Thread> threads;
            readonly DynamicSemaphore dynamicSemaphore;

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

        public class MultithreadedReadWriteEngine
        {
            public MultithreadedReadWriteEngine()
            {
                executeThread = new Thread(Execute) { IsBackground = true };
                blocksCount = Environment.ProcessorCount - 1;
                readThreadPool = new QueuedThreadPool(blocksCount);
                blocksToWrite = new List<Block>();
                _lockCompress = new object();
                ResetEvent = new ManualResetEvent(true);
            }

            readonly ManualResetEvent ResetEvent;
            readonly Thread executeThread;
            readonly QueuedThreadPool readThreadPool;
            int blocksCount;
            List<Block> blocksToWrite;
            object _lockCompress;

            public void Wait()
            {
                ResetEvent.WaitOne();
            }

            void CompressStreamToBlockData(Stream readStream, Block block)
            {
                using (var inputStream = new MemoryStream(ReadByteBlock(readStream, block.Size, block.Number)))
                {
                    using (var compressedStream = new MemoryStream())
                    {
                        using (var gz = new GZipStream(compressedStream, CompressionMode.Compress))
                        {
                            inputStream.CopyTo(gz);
                        }
                        block.Data = compressedStream.ToArray();
                        block.Readed = true;
                        lock (_lockCompress)
                        {
                            blocksToWrite.Add(block);
                            while (blocksToWrite.Count > blocksCount)
                            {
                                Monitor.Wait(_lockCompress);
                            }
                        }
                    }
                }
            }

            byte[] ReadByteBlock(Stream inputStream, int size, int blockNum)
            {
                var readedBytes = new byte[size];
                inputStream.Position = size * blockNum;
                inputStream.Read(readedBytes, 0, size);
                return readedBytes;
            }

            public void Execute()
            {
                int expectedBlock = 0;
                int lastBlockNum = 0;
                var currentLength = new FileInfo(@"C:\Backups\4k.jpg").Length;
                var blockSize = 1024 * 1024;

                //создание очереди сжатия блоков
                while (currentLength > 0)
                {
                    var newBlock = new Block(lastBlockNum, blockSize);
                    readThreadPool.AddActionToQueue(() =>
                    {
                        using (var readStream = new FileStream(@"C:\Backups\4k.jpg", FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            CompressStreamToBlockData(readStream, newBlock);
                        }
                    });
                    currentLength -= blockSize;
                    lastBlockNum++;
                }

                using (var outputStream = new FileStream(@"C:\Backups\4k.jpg.gz", FileMode.Create, FileAccess.Write))
                {
                    while (expectedBlock < lastBlockNum)
                    {
                        var isWait = true;
                        while (isWait)
                        {
                            lock (_lockCompress)
                            {
                                isWait = blocksToWrite.FirstOrDefault(w => w.Number == expectedBlock && w.Readed) == null;
                            }
                        }

                        Block block = null;

                        lock (_lockCompress)
                            block = blocksToWrite.First(w => w.Number == expectedBlock && w.Readed);

                        outputStream.Write(block.Data, 0, block.Data.Length);
                        lock (_lockCompress)
                        {
                            blocksToWrite.Remove(block);
                            Monitor.Pulse(_lockCompress);
                        }
                        expectedBlock++;
                    }
                }
                ResetEvent.Set();
            }
        }
    }
}