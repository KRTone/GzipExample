namespace MultithreadedGZip.BLL.Common
{
    public class Block
    {
        public Block(int number, int size)
        {
            Number = number;
            Size = size;
        }

        public int Number { get; }
        public byte[] Data { get; set; }
        public int Size { get; }
        public bool Readed { get; set; }
    }
}
