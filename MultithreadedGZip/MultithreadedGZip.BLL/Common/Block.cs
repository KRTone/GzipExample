namespace MultithreadedGZip.BLL.Common
{
    public class Block
    {
        public int Number { get; }
        public byte[] Bytes { get; }
        public int Size { get; }

        public Block(int number, byte[] bytes, int size)
        {
            Number = number;
            Bytes = bytes;
            Size = size;
        }
    }
}
