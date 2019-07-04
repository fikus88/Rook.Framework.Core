using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Rook.Framework.Core.Common;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class RookLogger : ILogger
	{
		private readonly Common.ILogger _rookFrameworkLogger;
		private readonly string _name;
		private readonly Dictionary<LogLevel, Common.LogLevel> _logMappings = new Dictionary<LogLevel, Common.LogLevel> {
			{ LogLevel.Trace, Common.LogLevel.Trace },
			{ LogLevel.Debug, Common.LogLevel.Debug },
			{ LogLevel.Information, Common.LogLevel.Info },
			{ LogLevel.Warning, Common.LogLevel.Warn },
			{ LogLevel.Error, Common.LogLevel.Error },
			{ LogLevel.Critical, Common.LogLevel.Fatal }
		};

		public RookLogger(Common.ILogger rookFrameworkLogger, string name)
		{
			_rookFrameworkLogger = rookFrameworkLogger;
			_name = name;
		}

		public IDisposable BeginScope<TState>(TState state)
		{
			return null;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return _rookFrameworkLogger.CurrentLogLevel == _logMappings[logLevel];
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			var message = formatter(state, exception);

			var operation = $"{_name}[{eventId.Id}]";
			var logItem = new LogItem("Message", message);

			Action<string, LogItem[]> log = ((op, logItems) => {});

			switch (logLevel)
			{
				case LogLevel.Trace:
					log = _rookFrameworkLogger.Trace;
					break;
				case LogLevel.Debug:
					log = _rookFrameworkLogger.Debug;
					break;
				case LogLevel.Information:
					log = _rookFrameworkLogger.Info;
					break;
				case LogLevel.Warning:
					log = _rookFrameworkLogger.Warn;
					break;
				case LogLevel.Error:
					log = _rookFrameworkLogger.Error;
					break;
				case LogLevel.Critical:
					log = _rookFrameworkLogger.Fatal;
					break;
				case LogLevel.None:
					break;
				default:
					log = _rookFrameworkLogger.Info;
					_rookFrameworkLogger.Warn($"{nameof(RookLogger)}.{nameof(Log)}", new LogItem("Message", "Invalid LogLevel specified"));
					break;
			}

			log(operation, new [] { logItem });
		}
	}
}
