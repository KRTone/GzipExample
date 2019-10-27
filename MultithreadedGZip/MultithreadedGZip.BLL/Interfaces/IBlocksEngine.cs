using MultithreadedGZip.BLL.Common;
using System.IO;
using System.Threading;

namespace MultithreadedGZip.BLL.Interfaces
{
    public interface IBlocksEngine
    {
        void HandleBlock(Block block, Stream outStream);
        void EndOfBlocks();
        ManualResetEvent Awaiter { get; }
    }
}
