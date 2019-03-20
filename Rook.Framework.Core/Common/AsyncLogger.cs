using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Rook.Framework.Core.Monitoring;

namespace Rook.Framework.Core.Common
{
	public enum LogLevel
	{
		Off,
		Fatal,
		Error,
		Warn,
		Info,
		Debug,
		Trace
	}

    public sealed class AsyncLogger : ILogger
    {
        private readonly LogLevel _defaultLogLevel;
        private readonly Timer _logTimer;

        public AsyncLogger(IDateTimeProvider dateTimeProvider, IConfigurationManager configurationManager,
            ILoggingMetrics loggingMetrics)
        {
            _dateTimeProvider = dateTimeProvider;
            _loggingMetrics = loggingMetrics;

            if (!Enum.TryParse(configurationManager.Get<string>("LogLevel", "Warn"), out _defaultLogLevel))
                _defaultLogLevel = LogLevel.Warn;
            
            _logTimer = new Timer(s=>DumpLogs(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        ~AsyncLogger()
        {
            DumpLogs();
        }

        private LogLevel LogLevel
        {
            get
            {
                return _defaultLogLevel;
            }
        } 

        private class LogEntry
        {
            public LogLevel LogLevel { get; }

            private readonly string _time;
            private readonly string _operation;
            private readonly LogItem[] _items;

            public LogEntry(LogLevel logLevel, IDateTimeProvider dateTimeProvider, string operation, LogItem[] items)
            {
                LogLevel = logLevel;

                _time = dateTimeProvider.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                LogLevel = logLevel;
                _operation = operation;
                _items = items;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder($"|{LogLevel}| ");
                
                sb.Append($"Time=\"{_time}\" ");
                sb.Append($"Service=\"{ServiceInfo.Name}\" ");
                sb.Append($"Operation=\"{_operation}\" ");

                foreach (var log in _items)
                {
                    var value = log.Value() ?? "null";
                    sb.Append($"{log.Key}=\"{value.Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", " ")}\" ");
                }

                return sb.ToString().TrimEnd(' ');
            }
        }

        private List<LogEntry> _pendingLogs = new List<LogEntry>();
        private readonly ILoggingMetrics _loggingMetrics;

        private readonly IDateTimeProvider _dateTimeProvider;

        private void DumpLogs()
        {
            List<LogEntry> currentLogs;
            lock (_pendingLogs)
            {
                currentLogs = _pendingLogs;
                _pendingLogs = new List<LogEntry>();
            }

            foreach (LogEntry log in currentLogs)
            {
                Console.WriteLine(log.ToString());
                _loggingMetrics.RecordLogMessage(log.LogLevel);
            }
        }

        private AsyncLogger() { }


        public void Trace(string operation, params LogItem[] logItems)
        {
            if (LogLevel >= LogLevel.Trace)
                lock (_pendingLogs)
                    _pendingLogs.Add(new LogEntry(LogLevel.Trace, _dateTimeProvider, operation, logItems));
        }

        public void Info(string operation, params LogItem[] logItems)
        {
            if (LogLevel >= LogLevel.Info)
                lock (_pendingLogs)
                    _pendingLogs.Add(new LogEntry(LogLevel.Info, _dateTimeProvider, operation, logItems));
        }

        public void Warn(string operation, params LogItem[] logItems)
        {
            if (LogLevel >= LogLevel.Warn)
                lock (_pendingLogs)
                    _pendingLogs.Add(new LogEntry(LogLevel.Warn, _dateTimeProvider, operation, logItems));
        }

        public void Error(string operation, params LogItem[] logItems)
        {
            if (LogLevel >= LogLevel.Error)
                lock (_pendingLogs)
                    _pendingLogs.Add(new LogEntry(LogLevel.Error, _dateTimeProvider, operation, logItems));
        }

        public void Fatal(string operation, params LogItem[] logItems)
        {
            if (LogLevel >= LogLevel.Fatal)
                lock (_pendingLogs)
                    _pendingLogs.Add(new LogEntry(LogLevel.Fatal, _dateTimeProvider, operation, logItems));
        }

        public void Debug(string operation, params LogItem[] logItems)
        {
            if (LogLevel >= LogLevel.Debug)
                lock (_pendingLogs)
                    _pendingLogs.Add(new LogEntry(LogLevel.Debug, _dateTimeProvider, operation, logItems));
        }

        public void Exception(string operation, string @event, Exception exception, params LogItem[] additionalLogItems)
        {
            if (LogLevel >= LogLevel.Error)
            {
                if (exception == null)
                    throw new ArgumentNullException(nameof(exception));

                LogItem[] exceptionLogItems = UnpackExceptionToLogItems(@event, exception);

                if (additionalLogItems != null && additionalLogItems.Any())
                    exceptionLogItems = exceptionLogItems.Concat(additionalLogItems).ToArray();

                Error(operation, exceptionLogItems);
            }
        }

        private static LogItem[] UnpackExceptionToLogItems(string @event, Exception exception)
        {
            var exceptionLogItems = new[]
            {
                new LogItem("Event", @event),
                new LogItem("ExceptionType", exception.GetType().FullName),
                new LogItem("Message", exception.ToString),
                new LogItem("StackTrace", exception.StackTrace)
            };
            return exceptionLogItems;
        }

        public LogLevel CurrentLogLevel => LogLevel;
    }
}