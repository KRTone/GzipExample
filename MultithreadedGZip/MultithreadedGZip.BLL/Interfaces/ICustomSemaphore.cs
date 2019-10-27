namespace MultithreadedGZip.BLL.Interfaces
{
    public interface ICustomSemaphore
    {
        void WaitOne();
        void Release();
        int CurrentCount { get; }
    }
}
