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
    public class MultithreadedCompressor : MultithreadedGZipExecutor
    {
        public MultithreadedCompressor(IMultithreadedConfigurator mcfg, IGZipConfigurator gzcfg, ILogService logService)
            : base(gzcfg.InFilePath, gzcfg.OutFilePath, mcfg.BlockSize, mcfg.Processors, logService)
        {
        }

        protected override void InternalExecute()
        {
            int expectedBlock = 0;
            int lastBlockNum = 0;
            var currentLength = new FileInfo(inputFilePath).Length;

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
                    Dictionary<int, long> numLengthDict = new Dictionary<int, long>();
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
                        //сюда можно добавить сапись в кастомный хедер, длины сжатых блоков (см. примечание для разжатия)
                        outputStream.Write(block.Data, 0, block.Data.Length);
                        lock (blocksToWrite)
                        {
                            numLengthDict.Add(block.Number, block.Data.Length);
                            blocksToWrite.Remove(block);
                            Monitor.Pulse(blocksToWrite);
                        }
                        expectedBlock++;
                    }
                    long value = 0;
                    foreach (var pos in numLengthDict)
                    {
                        value += pos.Value;
                        sw.Write(value);
                    }
                }
            }
        }

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
    }
}
