using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backupwordservice.log
{
    public class CustomFileLoggerConfiguration
    {
        public CustomFileLoggerConfiguration()
        {
            MinLevel = LogLevel.Debug;
        }

        public LogLevel LogLevel { get; set; }

        public LogLevel MinLevel { get; set; }

        public string LogPath { get; set; }
    }
}