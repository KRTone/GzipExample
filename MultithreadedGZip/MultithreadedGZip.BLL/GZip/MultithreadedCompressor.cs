using MultithreadedGZip.BLL.Common;
using MultithreadedGZip.BLL.MultithreadedExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace MultithreadedGZip.BLL.GZip
{
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
}
