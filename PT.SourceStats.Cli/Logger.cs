using Newtonsoft.Json;
using System;
using NLog;
using ILogger = PT.PM.Common.ILogger;
using LogLevel = PT.PM.Common.LogLevel;

namespace PT.SourceStats.Cli
{
    public class Logger : ILogger
    {
        private readonly NLog.Logger consoleLogger = LogManager.GetLogger("console");
        private readonly NLog.Logger fileLogger = LogManager.GetLogger("file");
        private int errorCount;

        public int ErrorCount => errorCount;

        public string LogsDir { get; set; }

        public LogLevel LogLevel { get; set; }

        public void LogDebug(string message)
        {
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
                if (message.MessageType == MessageType.Result && LogLevel >= LogLevel.Off ||
                    message.MessageType == MessageType.Error && LogLevel >= LogLevel.Error ||
                    message.MessageType == MessageType.Progress && LogLevel >= LogLevel.Info)
                {
                    var json = JsonConvert.SerializeObject(message, Formatting.Indented);
                    consoleLogger.Info(json);
                    fileLogger.Info(json);
                }
            }
        }

        public void LogInfo(string message)
        {
            consoleLogger.Info(message);
        }

        public Logger()
        {
            errorCount = 0;
        }
    }
}
