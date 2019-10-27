using MultithreadedGZip.BLL.Interfaces;
using System;

namespace MultithreadedGZip.Loggers
{
    public class ConsoleLogger : ILogger
    {
        public void Exception(Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine("Stack trace:");
            Console.WriteLine(ex.StackTrace);
        }

        public void Info(string message)
        {
            Console.WriteLine("Info: " + message);
        }
    }
}
