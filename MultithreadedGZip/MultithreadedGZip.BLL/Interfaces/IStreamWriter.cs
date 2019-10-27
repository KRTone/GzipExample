using System.IO;

namespace MultithreadedGZip.BLL.Interfaces
{
    public interface IStreamWriter
    {
        void Write(Stream stream, Stream outStream);
    }
}
