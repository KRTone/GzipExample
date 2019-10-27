using MultithreadedGZip.BLL.Common;
using MultithreadedGZip.BLL.Interfaces;
using MultithreadedGZip.BLL.Interfaces.Configurators;
using System;
using System.IO;
using System.Threading;

namespace MultithreadedGZip.BLL.Writers
{
    public class BytesToBlockWriter : IBlockWriter
    {
        int nextBlockNumber;
        readonly byte[][] buffer;
        readonly object locker;
        readonly ILogService logger;
        readonly IMultithreadedConfigurator configurator;

        public BytesToBlockWriter(IMultithreadedConfigurator configurator, ILogService logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));

            if (configurator.Processors < 0) throw new ArgumentOutOfRangeException(nameof(configurator.Processors));

            buffer = new byte[configurator.Processors][];
            locker = new object();
        }

        public void Write(Block block, Stream outStream)
        {
            if (block.Number < nextBlockNumber) ThrowBlockAlreadyWritten(block.Number);
            if (block.Bytes == null) throw new ArgumentNullException(nameof(block.Bytes));

            lock (locker)
            {
                int maxBlockNumber = nextBlockNumber + buffer.Length;
                while (block.Number > maxBlockNumber || block.Number == maxBlockNumber && buffer.Length != 0)
                {
                    Monitor.Wait(locker);
                    maxBlockNumber = nextBlockNumber + buffer.Length;
                }

                logger.Info($"BytesToBlockWriter.Write() BlockNumber = {block.Number}");

                if (buffer.Length == 0)
                {
                    WriteBytes(outStream, block.Bytes);
                    ShiftBlocks(1);
                    return;
                }

                int bufferIndex = block.Number - nextBlockNumber;
                if (buffer[bufferIndex] != null)
                {
                    ThrowBlockAlreadyWritten(block.Number);
                }

                buffer[block.Number - nextBlockNumber] = block.Bytes;
                if (block.Number == nextBlockNumber)
                {
                    int shift = CalculateShift();
                    WriteBlocks(shift, outStream);
                    ShiftBlocks(shift);
                }
            }
        }

        void ThrowBlockAlreadyWritten(int blockNumber)
        {
            var ex = new ArgumentOutOfRangeException($"Block with same number ({blockNumber}) has already written");
            logger.Exception(ex);
            throw ex;
        }

        void ShiftBlocks(int shift)
        {
            for (int i = 0; i < buffer.Length; ++i)
            {
                if (i >= shift)
                    buffer[i - shift] = buffer[i];
                buffer[i] = null;
            }
            nextBlockNumber += shift;
            Monitor.PulseAll(locker);
        }

        void WriteBlocks(int shift, Stream outStream)
        {
            for (int i = 0; i < shift; ++i)
            {
                WriteBytes(outStream, buffer[i]);
            }
        }

        void WriteBytes(Stream outStream, byte[] bytes)
        {
            using (MemoryStream inStream = new MemoryStream(bytes))
            {
                inStream.CopyTo(outStream);
            }
        }

        int CalculateShift()
        {
            int shift = 0;
            while (shift < buffer.Length && buffer[shift] != null)
                ++shift;
            return shift;
        }
    }
}