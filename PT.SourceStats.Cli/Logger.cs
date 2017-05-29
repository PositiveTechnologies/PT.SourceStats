using Newtonsoft.Json;
using System;
using PT.PM.Common.CodeRepository;
using NLog;

namespace PT.SourceStats.Cli
{
    public class Logger : PT.PM.Common.ILogger
    {
        private NLog.Logger ConsoleLogger = LogManager.GetLogger("console");
        private NLog.Logger FileLogger = LogManager.GetLogger("file");
        private int errorCount;

        public int ErrorCount => errorCount;

        public LogLevel LogLevel { get; set; }

        public ISourceCodeRepository SourceCodeRepository { get; set; }

        public void LogDebug(string message)
        {
        }

        public void LogError(string message)
        {
            errorCount++;
            LogInfo(new ErrorMessage(message));
        }

        public void LogError(Exception ex)
        {
            errorCount++;
            LogInfo(new ErrorMessage(ex.ToString()));
        }

        public void LogInfo(object infoObj)
        {
            if (infoObj is Message message)
            {
                if (message.MessageType == MessageType.Error)
                {
                    errorCount++;
                }
                if ((message.MessageType == MessageType.Progress && LogLevel <= LogLevel.All) ||
                    (message.MessageType == MessageType.Error && LogLevel <= LogLevel.Errors) ||
                    (message.MessageType == MessageType.Result && LogLevel <= LogLevel.Result))
                {
                    var json = JsonConvert.SerializeObject(message, Formatting.Indented);
                    ConsoleLogger.Info(json);
                    FileLogger.Info(json);
                }
            }
        }

        public void LogInfo(string message)
        {
            ConsoleLogger.Info(message);
        }

        public Logger()
        {
            errorCount = 0;
        }
    }
}
