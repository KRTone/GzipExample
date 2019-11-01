using MultithreadedGZip.BLL.Common;
using MultithreadedGZip.BLL.Configurators;
using MultithreadedGZip.BLL.Interfaces;
using MultithreadedGZip.BLL.Interfaces.Configurators;
using MultithreadedGZip.BLL.MultithreadedExtensions;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace MultithreadedGZip.BLL.GZip
{
    public class MultithreadedDecompressor : MultithreadedGZipExecutor
    {
        public MultithreadedDecompressor(IMultithreadedConfigurator mcfg, IGZipConfigurator gzcfg, ILogService logService)
            : base(gzcfg.InFilePath, gzcfg.OutFilePath, mcfg.BlockSize, mcfg.Processors, logService)
        {
        }
        
        object _lockStreamRead = new object();

        void ReadBlocksEndPostions(Dictionary<int, long> numLengthDict, int maxBlocksNum)
        {
            int indx = 0;
            using (var input = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            using (var sr = new BinaryReader(input))
            {
                input.Position = input.Length - ((maxBlocksNum + 1) * 8);
                while (maxBlocksNum > indx)
                {
                    numLengthDict.Add(indx++, sr.ReadInt64());
                }
            }
        }

        protected override void InternalExecute()
        {
            int expectedBlock = 0;
            int lastBlockNum = 0;
            int maxBlocksNum = 0;
            Dictionary<int, long> numLengthDict = new Dictionary<int, long>();

            //чтение количества блоков
            using (var sr = new BinaryReader(new FileStream(inputFilePath, FileMode.Open, FileAccess.Read)))
            {
                maxBlocksNum = sr.ReadInt32();
            }
            //чтение позиции блоков в сжатом файле
            ReadBlocksEndPostions(numLengthDict, maxBlocksNum);

            //создание очереди разархивирования блоков
            while (maxBlocksNum >= lastBlockNum)
            {
                var newBlock = new Block(lastBlockNum, blockSize);
                readThreadPool.AddActionToQueue(() =>
                {
                    using (var readStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        DecompressStreamToBlockData(readStream, newBlock,
                            newBlock.Number == 0 ? 4 : numLengthDict[newBlock.Number - 1] + 4,
                            newBlock.Number == maxBlocksNum ? readStream.Length : numLengthDict[newBlock.Number] + 4);
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

        void DecompressStreamToBlockData(Stream readStream, Block block, long startPos, long endPos)
        {
            readStream.Position = startPos;
            var length = endPos - startPos;
            var data = new byte[length];
            readStream.Read(data, 0, (int)length);
            
            using (var inputStream = new MemoryStream(data))
            {
                using (var decompressedStream = new MemoryStream())
                {
                    using (var gz = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        gz.CopyTo(decompressedStream);
                    }
                    block.Data = decompressedStream.ToArray();
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
    }
}
