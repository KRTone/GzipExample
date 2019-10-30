using System;
using System.IO;
using System.IO.Compression;
using MultithreadedGZip.BLL.Interfaces;
using MultithreadedGZip.BLL.Interfaces.Configurators;

namespace MultithreadedGZip.BLL.GZip
{
    //public class GZipDecompressor : GZipBase
    //{
    //    public GZipDecompressor(IBlockWriter writer, IGZipConfigurator zipConfigurator, ILogService logService) : base(writer, zipConfigurator)
    //    {
    //        this.logService = logService ?? throw new ArgumentNullException(nameof(logService));
    //    }

    //    readonly ILogService logService;

    //    protected override Lazy<Stream> InputStream => new Lazy<Stream>(() =>
    //    {
    //        return new GZipStream(File.OpenRead(zipConfigurator.InFilePath), CompressionMode.Decompress);
    //    });

    //    protected override Lazy<Stream> OutStream => new Lazy<Stream>(() =>
    //    {
    //        return new FileStream(zipConfigurator.OutFilePath, FileMode.Create, FileAccess.ReadWrite);
    //    });

    //    protected override void ExecuteDispose()
    //    {
    //        base.ExecuteDispose();
    //        logService.Info("GZipDecompressor.ExecuteDispose()");
    //    }
    //}
}
