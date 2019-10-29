using MultithreadedGZip.BLL.Common;

namespace MultithreadedGZip.BLL.Interfaces
{
    public interface ICompressor
    {
        int ReadBytes(byte[] buffer, int blockSize);
        void WriteBlock(Block block);
    }
}
