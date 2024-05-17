using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backupwordservice.log
{
    public class CoustomFileLogger : ILogger
    {

        private readonly string _name;
        private readonly CustomFileLoggerConfiguration _config;
        private LogLevel _logLevel;
        private string categoryName;

        public CoustomFileLogger(string categoryName)
        {
            this.categoryName = categoryName;
        }

        public CoustomFileLogger(string name, CustomFileLoggerConfiguration config)
        {
            _name = name;
            _config = config;
        }


        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == _config.LogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if(!IsEnabled(logLevel)) 
            {
                return;
            }
            _logLevel = logLevel;
            FileLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {logLevel} {formatter(state, exception)}\n");
        }

        private void FileLog(string strLog) 
        {
            string fileName = DateTime.Now.ToString("yyyy-MM") + ".log";
            string filePath = _config.LogPath + "\\" + fileName;
            File.AppendAllTextAsync(filePath, strLog);
        }
    }
}