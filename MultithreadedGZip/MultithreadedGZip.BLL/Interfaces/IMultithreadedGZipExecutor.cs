namespace MultithreadedGZip.BLL.Interfaces
{
    public interface IMultithreadedGZipExecutor
    {
        void Execute(bool wait);
        void Wait();
    }
}