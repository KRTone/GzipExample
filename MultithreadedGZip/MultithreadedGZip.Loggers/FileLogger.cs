using System;
using log4net;
using log4net.Config;
using MultithreadedGZip.BLL.Interfaces;

namespace MultithreadedGZip.Loggers
{
    public class FileLogger : ILogger
    {
        ILog log = LogManager.GetLogger("gzip");

        public FileLogger()
        {
        }

        public void Exception(Exception ex)
        {
            log.Error(ex.Message);
        }

        public void Info(string message)
        {
            log.Info(message);
        }
    }
}
