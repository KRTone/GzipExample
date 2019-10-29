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
        }

        readonly ILogService logService;

        protected override Lazy<Stream> InputStream => new Lazy<Stream>(() => 
        {
            return File.OpenRead(zipConfigurator.InFilePath);
        });

        protected override Lazy<Stream> OutStream => new Lazy<Stream>(() =>
        {
            return new GZipStream(new FileStream(zipConfigurator.OutFilePath, FileMode.Create, FileAccess.ReadWrite), CompressionMode.Compress);
        });

        protected override void ExecuteDispose()
        {
            base.ExecuteDispose();
            logService.Info("GZipCompressor.ExecuteDispose()");
        }
    }
}
