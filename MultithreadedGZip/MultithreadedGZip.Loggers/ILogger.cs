namespace MultithreadedGZip.Loggers
{
    public interface ILogger
    {
        void Info(string message);
        void Exception(System.Exception ex);
    }
}
