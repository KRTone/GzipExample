using MultithreadedGZip.BLL.Common;
using System.IO;

namespace MultithreadedGZip.BLL.Interfaces
{
    public interface IBlockWriter
    {
        void Write(Block block, Stream outStream);
    }
}
