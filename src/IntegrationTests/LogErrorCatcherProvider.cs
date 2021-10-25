using System;
using Microsoft.Extensions.Logging;
using MyLab.Log;

namespace IntegrationTests
{
    class LogErrorCatcherProvider : ILoggerProvider
    {
        private readonly LogErrorCatcher _instance;

        public LogErrorCatcherProvider(LogErrorCatcher instance)
        {
            _instance = instance;
        }

        public void Dispose()
        {
            
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _instance;
        }
    }

    class LogErrorCatcher : ILogger
    {
        public LogEntity LastError { get; set; }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.Error)
                LastError = state as LogEntity;

        }
    }
}
