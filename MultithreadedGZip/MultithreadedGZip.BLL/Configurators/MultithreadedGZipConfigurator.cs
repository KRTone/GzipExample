using System.IO.Compression;
using MultithreadedGZip.BLL.Interfaces.Configurators;

namespace MultithreadedGZip.BLL.Configurators
{
    public class MultithreadedGZipConfigurator : IMultithreadedConfigurator, IGZipConfigurator
    {
        public MultithreadedGZipConfigurator(
            int processors,
            string inFilePath,
            string outFilePath,
            CompressionMode compressionMode,
            int blockSize)
        {
            Processors = processors;
            InFilePath = inFilePath;
            OutFilePath = outFilePath;
            CompressionMode = compressionMode;
            BlockSize = blockSize;
        }

        public int Processors { get; }
        public string InFilePath { get; }
        public string OutFilePath { get; }
        /// <summary>
        /// In bytes
        /// </summary>
        public int BlockSize { get; }
        public CompressionMode CompressionMode { get; }
    }
}
