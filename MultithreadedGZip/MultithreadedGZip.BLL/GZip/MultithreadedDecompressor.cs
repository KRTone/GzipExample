using MultithreadedGZip.BLL.Common;
using MultithreadedGZip.BLL.Configurators;
using MultithreadedGZip.BLL.Interfaces;
using MultithreadedGZip.BLL.Interfaces.Configurators;
using MultithreadedGZip.BLL.MultithreadedExtensions;
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

        long streamPosition;
        int expetedBlockNum;
        object _lockStreamRead = new object();

        protected override void InternalExecute()
        {
            int expectedBlock = 0;
            int lastBlockNum = 0;
            int maxLength = 0;
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
    }
}
