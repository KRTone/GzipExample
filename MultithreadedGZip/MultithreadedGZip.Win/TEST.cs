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
            MultithreadedGZipExecutor ex = new MultithreadedCompressor(@"", @"");
            ex.Execute(true);
            MultithreadedGZipExecutor ex2 = new MultithreadedDecompressor(@"", @"");
            ex2.Execute(true);
        }

        public static void Decompress(FileInfo fileToDecompress)
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                        Console.WriteLine($"Decompressed: {fileToDecompress.Name}");
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

                //один поток на запись на протяжении всего цикла
                //остальные заняты разархивированием
                blocksToWriteCount = Environment.ProcessorCount - 1;
                readThreadPool = new QueuedThreadPool(blocksToWriteCount);
                blocksToWrite = new List<Block>();
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

            protected readonly QueuedThreadPool readThreadPool;
            protected readonly int blocksToWriteCount;
            protected readonly List<Block> blocksToWrite;

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
            }
            object _lockRead = new object();

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
                        Console.WriteLine($"Dec {block.Number} {block.Data.Length}");
                        block.Readed = true;
                    }
                }

                //пока идет запись и n блоков еще не записаны, 
                //нет смысла зиповать другие блоки и нагружать оперативку
                lock (blocksToWrite)
                {
                    blocksToWrite.Add(block);
                    while (blocksToWrite.Count > blocksToWriteCount)
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
                    using (var sw = new BinaryWriter(outputStream))
                    {
                        //запишем количество блоков необходимых для распаковки
                        sw.Write(lastBlockNum - 1);

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

                            //после того как записали блок и очистили данные даем команду начинать зиповать следующий блок
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

        public class MultithreadedDecompressor : MultithreadedGZipExecutor
        {
            public MultithreadedDecompressor(string inputFilePath, string outputFilePath) : base(inputFilePath, outputFilePath)
            {
            }

            long streamPosition;
            int expetedBlockNum;
            object _lockStreamRead = new object();

            void DecompressStreamToBlockData(Stream readStream, Block block)
            {
                while (expetedBlockNum != block.Number)
                {
                    lock (_lockStreamRead)
                    {
                        Monitor.Wait(_lockStreamRead);
                    }
                }

                using (var inputStream = GetCompressedBlock(readStream, new MemoryStream(), streamPosition))
                {
                    //меняем позицию для чтения и выводим из сна следующий блок
                    lock (_lockStreamRead)
                    {
                        expetedBlockNum++;
                        streamPosition = readStream.Position;
                        Monitor.PulseAll(_lockStreamRead);
                    }

                    using (var decompressedStream = new MemoryStream())
                    {
                        using (var gz = new GZipStream(inputStream, CompressionMode.Decompress))
                        {
                            gz.CopyTo(decompressedStream);
                        }
                        block.Data = decompressedStream.ToArray();
                        Console.WriteLine($"Dec {block.Number} {block.Data.Length}");
                        block.Readed = true;
                    }
                }

                lock (blocksToWrite)
                {
                    blocksToWrite.Add(block);
                    while (blocksToWrite.Count > blocksToWriteCount)
                    {
                        Monitor.Wait(blocksToWrite);
                    }
                }
            }

            //извлечение зазипованных блоков в файле требует оптимизирования
            //проблема в том что им добавляется header и suffix зипа (это надо копать, это уже совсем другое тз)
            //можно даже начать читать подобные мануалы https://tools.ietf.org/html/rfc1952 и это займет уйму времени
            //чем хуже сжаты блоки тем дольше выполняется данный код (чем длиннее зазипованный блок данных)
            //Возможное решение: например можно вслед за байтами хранящисми значение количества блоков
            //вписывать long значения позиций где находятся эти блоки и затем мы не будем привязаны к тому
            //что будем ожидать, пока границы предыдущего блока не будут выявлены,
            //это уже какой-то свой +-полноценный зипер будет :D 
            //(я раньше с зипованием не работал, поэтому знания не сильно глубоки в этой области и мб чего-то еще не догоняю)
            MemoryStream GetCompressedBlock(Stream inputStream, MemoryStream outStream, long streamStartPos)
            {
                bool isCurrent = true;
                inputStream.Position = streamStartPos;
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
                outStream.Position = 0;
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
                int maxLength = 0;
                var blockSize = 1024 * 1024;
                using (var sr = new BinaryReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read)))
                {
                    maxLength = sr.ReadInt32();
                }

                //пропускаем значение хранящее количества блоков
                streamPosition = 4;

                while (maxLength >= lastBlockNum)
                {
                    var newBlock = new Block(lastBlockNum, blockSize);
                    readThreadPool.AddActionToQueue(() =>
                    {
                        using (var readStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            DecompressStreamToBlockData(readStream, newBlock);
                        }
                    });
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