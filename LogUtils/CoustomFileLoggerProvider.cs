using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backupwordservice.log
{
    public class CoustomFileLoggerProvider : ILoggerProvider
    {
        private readonly CustomFileLoggerConfiguration _config;

        public CoustomFileLoggerProvider(CustomFileLoggerConfiguration config)
        {
            _config = config;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new CoustomFileLogger(categoryName, _config);
        }

        public void Dispose()
        {
            
        }
    }
}