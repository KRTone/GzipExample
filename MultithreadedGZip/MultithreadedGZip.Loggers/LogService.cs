using System;
using System.Collections.Generic;
using MultithreadedGZip.BLL.Interfaces;

namespace MultithreadedGZip.Loggers
{
    public class LogService : ILogService
    {
        public LogService(IEnumerable<ILogger> loggers)
        {
            this.loggers = loggers;
        }

        IEnumerable<ILogger> loggers;

        public void Exception(Exception ex)
        {
            foreach (var logger in loggers)
                logger.Exception(ex);
        }

        public void Info(string message)
        {
            foreach (var logger in loggers)
                logger.Info(message);
        }
    }
}
