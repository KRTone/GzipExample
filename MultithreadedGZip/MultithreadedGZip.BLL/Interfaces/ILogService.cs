namespace MultithreadedGZip.BLL.Interfaces
{
    public interface ILogService
    {
        void Info(string message);
        void Exception(System.Exception ex);
    }
}