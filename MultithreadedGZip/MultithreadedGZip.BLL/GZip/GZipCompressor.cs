using System;
using System.IO;
using System.IO.Compression;
using MultithreadedGZip.BLL.Interfaces;
using MultithreadedGZip.BLL.Interfaces.Configurators;

namespace MultithreadedGZip.BLL.GZip
{
    public class GZipCompressor : GZipBase
    {
        public GZipCompressor(IBlockWriter writer, IGZipConfigurator zipConfigurator, ILogService logService) : base(writer, zipConfigurator)
        {
            this.logService = logService ?? throw new ArgumentNullException(nameof(logService));
            outStream = new Lazy<Stream>(() => new GZipStream(new FileStream(zipConfigurator.OutFilePath, FileMode.Create, FileAccess.ReadWrite), CompressionMode.Compress));
            inputStream = new Lazy<Stream>(() => File.OpenRead(zipConfigurator.InFilePath));
        }

        readonly ILogService logService;
        readonly Lazy<Stream> inputStream;
        readonly Lazy<Stream> outStream;

        protected override Stream InputStream => inputStream.Value;


        protected override Stream OutStream => outStream.Value;

        protected override void ExecuteDispose()
        {
            base.ExecuteDispose();
            logService.Info("GZipCompressor.ExecuteDispose()");
        }
    }
}
