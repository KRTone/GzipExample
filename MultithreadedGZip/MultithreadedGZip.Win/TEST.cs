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
            MultithreadedGZipExecutor ex = new MultithreadedCompressor(@"C:\Backups\4k.jpg", @"C:\Backups\4k.jpg.gz");
            ex.Execute(true);
            MultithreadedGZipExecutor ex2 = new MultithreadedDecompressor(@"C:\Backups\4k.jpg.gz", @"C:\Backups\4k2.jpg");
            ex2.Execute(true);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.WriteLine((e.ExceptionObject as Exception).Message);
                Console.ReadKey();
            };
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

        public abstract class MultithreadedGZipExecutor
        {
            public MultithreadedGZipExecutor(string inputFilePath, string outputFilePath)
            {
                ThrowIsNullOrWhiteSpace(inputFilePath, out this.inputFilePath);
                ThrowIsNullOrWhiteSpace(outputFilePath, out this.outputFilePath);
                executeThread = new Thread(Execution) { IsBackground = true };
                resetEvent = new ManualResetEvent(false);
            }

            private void ThrowIsNullOrWhiteSpace(string inStr, out string outStr)
            {
                if (string.IsNullOrWhiteSpace(inStr))
                    throw new ArgumentNullException(inStr);
                outStr = inStr;
            }

            protected readonly string inputFilePath;
            protected readonly string outputFilePath;
            protected readonly Thread executeThread;
            readonly ManualResetEvent resetEvent;

            public virtual void Execute(bool wait)
            {
                executeThread.Start();
                if (wait)
                    resetEvent.WaitOne();
            }
            protected abstract void InternalExecute();
            public virtual void Wait()
            {
                resetEvent.WaitOne();
            }

            private void Execution()
            {
                InternalExecute();
                resetEvent.Set();
            }
        }

        public class MultithreadedCompressor : MultithreadedGZipExecutor
        {
            public MultithreadedCompressor(string inputFilePath, string outputFilePath) : base(inputFilePath, outputFilePath)
            {

                blocksCount = Environment.ProcessorCount - 1;
                readThreadPool = new QueuedThreadPool(blocksCount);
                blocksToWrite = new List<Block>();
            }

            readonly QueuedThreadPool readThreadPool;
            int blocksCount;
            readonly List<Block> blocksToWrite;

            void CompressStreamToBlockData(Stream readStream, Block block)
            {
                using (var inputStream = new MemoryStream(ReadByteBlock(readStream, block)))
                {
                    using (var compressedStream = new MemoryStream())
                    {
                        using (var gz = new GZipStream(compressedStream, CompressionMode.Compress))
                        {
                            inputStream.CopyTo(gz);
                        }
                        block.Data = compressedStream.ToArray();
                        block.Readed = true;
                    }
                }

                lock (blocksToWrite)
                {
                    blocksToWrite.Add(block);
                    while (blocksToWrite.Count > blocksCount)
                    {
                        Monitor.Wait(blocksToWrite);
                    }
                }

            }

            byte[] ReadByteBlock(Stream inputStream, Block block)
            {
                var readedBytes = new byte[block.Size];
                inputStream.Position = block.Size * block.Number;
                inputStream.Read(readedBytes, 0, block.Size);
                return readedBytes;
            }

            protected override void InternalExecute()
            {
                int expectedBlock = 0;
                int lastBlockNum = 0;
                var currentLength = new FileInfo(inputFilePath).Length;
                var blockSize = 1024 * 1024;

                //создание очереди сжатия блоков
                while (currentLength > 0)
                {
                    var newBlock = new Block(lastBlockNum, blockSize);
                    readThreadPool.AddActionToQueue(() =>
                    {
                        using (var readStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            CompressStreamToBlockData(readStream, newBlock);
                        }
                    });
                    currentLength -= blockSize;
                    lastBlockNum++;
                }

                using (var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                {
                    while (expectedBlock < lastBlockNum)
                    {
                        var isWait = true;
                        while (isWait)
                        {
                            lock (blocksToWrite)
                            {
                                isWait = blocksToWrite.FirstOrDefault(w => w.Number == expectedBlock && w.Readed) == null;
                            }
                        }

                        Block block = null;

                        lock (blocksToWrite)
                            block = blocksToWrite.First(w => w.Number == expectedBlock && w.Readed);

                        outputStream.Write(block.Data, 0, block.Data.Length);
                        lock (blocksToWrite)
                        {
                            blocksToWrite.Remove(block);
                            Monitor.Pulse(blocksToWrite);
                        }
                        expectedBlock++;
                    }
                }
            }
        }

        public class MultithreadedDecompressor : MultithreadedGZipExecutor
        {
            public MultithreadedDecompressor(string inputFilePath, string outputFilePath) : base(inputFilePath, outputFilePath)
            {

                blocksCount = Environment.ProcessorCount - 1;
                readThreadPool = new QueuedThreadPool(blocksCount);
                blocksToWrite = new List<Block>();
            }

            readonly QueuedThreadPool readThreadPool;
            int blocksCount;
            readonly List<Block> blocksToWrite;

            void DecompressStreamToBlockData(Stream readStream, Block block)
            {
                //using (var inputStream = new MemoryStream(ReadByteBlock(readStream, block)))
                using (var inputStream = GetCompressedBlock(readStream, new MemoryStream()))
                {
                    using (var decompressedStream = new MemoryStream())
                    {
                        using (var gz = new GZipStream(inputStream, CompressionMode.Decompress))
                        {
                            gz.CopyTo(decompressedStream);
                        }
                        var data = decompressedStream.ToArray();
                        block.Data = decompressedStream.ToArray();
                        block.Readed = true;
                    }
                }

                lock (blocksToWrite)
                {
                    blocksToWrite.Add(block);
                    while (blocksToWrite.Count > blocksCount)
                    {
                        Monitor.Wait(blocksToWrite);
                    }
                }

            }

            byte[] ReadByteBlock(Stream inputStream, Block block)
            {
                var readedBytes = new byte[block.Size];
                bool isFirstblock = true;
                while (isFirstblock)
                {
                    inputStream.Position = block.Size * block.Number;
                    inputStream.Read(readedBytes, 0, block.Size);
                    inputStream.ReadByte();

                }

                return readedBytes;
            }

            MemoryStream GetCompressedBlock(Stream inputStream, MemoryStream outStream)
            {
                bool isCurrent = true;
                while (inputStream.Position < inputStream.Length)
                {
                    if (Read(inputStream, outStream, 31))
                    {
                        if (Read(inputStream, outStream, 139))
                        {
                            if (Read(inputStream, outStream, 8))
                            {
                                if (Read(inputStream, outStream, 0))
                                {
                                    if (Read(inputStream, outStream, 0))
                                    {
                                        if (Read(inputStream, outStream, 0))
                                        {
                                            if (Read(inputStream, outStream, 0))
                                            {
                                                if (Read(inputStream, outStream, 0))
                                                {
                                                    if (Read(inputStream, outStream, 4))
                                                    {
                                                        if (Read(inputStream, outStream, 0))
                                                        {
                                                            if (isCurrent)
                                                            {
                                                                isCurrent = false;
                                                                continue;
                                                            }
                                                            inputStream.Position -= 10;
                                                            var data = outStream.ToArray().Take((int)outStream.Length - 10).ToArray();
                                                            return new MemoryStream(data);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return outStream;
            }

            bool Read(Stream inputStream, Stream outStream, byte comparingByte)
            {
                byte readedByte = (byte)inputStream.ReadByte();
                if (readedByte == comparingByte)
                {
                    outStream.WriteByte(comparingByte);
                    return true;
                }
                outStream.WriteByte(readedByte);
                return false;
            }

            protected override void InternalExecute()
            {
                int expectedBlock = 0;
                int lastBlockNum = 0;
                var currentLength = new FileInfo(inputFilePath).Length;
                var blockSize = 1024 * 1024;

                //создание очереди сжатия блоков
                while (currentLength > 0)
                {
                    var newBlock = new Block(lastBlockNum, blockSize);
                    readThreadPool.AddActionToQueue(() =>
                    {
                        using (var readStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            DecompressStreamToBlockData(readStream, newBlock);
                        }
                    });
                    currentLength -= blockSize;
                    lastBlockNum++;
                }

                using (var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                {
                    while (expectedBlock < lastBlockNum)
                    {
                        var isWait = true;
                        while (isWait)
                        {
                            lock (blocksToWrite)
                            {
                                isWait = blocksToWrite.FirstOrDefault(w => w.Number == expectedBlock && w.Readed) == null;
                            }
                        }

                        Block block = null;

                        lock (blocksToWrite)
                            block = blocksToWrite.First(w => w.Number == expectedBlock && w.Readed);

                        outputStream.Write(block.Data, 0, block.Data.Length);
                        lock (blocksToWrite)
                        {
                            blocksToWrite.Remove(block);
                            Monitor.Pulse(blocksToWrite);
                        }
                        expectedBlock++;
                    }
                }
            }
        }
    }
}