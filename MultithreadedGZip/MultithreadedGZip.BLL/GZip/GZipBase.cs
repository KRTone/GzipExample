using MultithreadedGZip.BLL.Common;
using MultithreadedGZip.BLL.Interfaces;
using MultithreadedGZip.BLL.Interfaces.Configurators;
using System;
using System.IO;

namespace MultithreadedGZip.BLL.GZip
{
    public abstract class GZipBase : IDisposable, ICompressor
    {
        public GZipBase(IBlockWriter writer, IGZipConfigurator zipConfigurator)
        {
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
            this.zipConfigurator = zipConfigurator ?? throw new ArgumentNullException(nameof(zipConfigurator));
        }

        protected readonly IBlockWriter writer;
        abstract protected Stream InputStream { get; }
        abstract protected Stream OutStream { get; }
        protected readonly IGZipConfigurator zipConfigurator;
        readonly object _lock = new object();

        public int ReadBytes(byte[] buffer, int blockSize) =>
            InputStream.Read(buffer, 0, blockSize);

        public void WriteBlock(Block block) =>
                writer.Write(block, OutStream);

        protected virtual void ExecuteDispose()
        {
            InputStream.Dispose();
            OutStream.Dispose();
        }

        bool isDisposed = false;
        public void Dispose()
        {
            if (!isDisposed)
            {
                ExecuteDispose();
            }

        }
    }
}
