using System.IO.Compression;

namespace MultithreadedGZip.BLL.Interfaces.Configurators
{
    public interface IGZipConfigurator
    {
        string InFilePath { get; }
        string OutFilePath { get; }
        CompressionMode CompressionMode { get; }
    }
}
