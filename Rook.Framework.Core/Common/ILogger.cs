namespace Rook.Framework.Core.Common
{
	public interface ILogger
	{
		void Trace(string operation, params LogItem[] logItems);
		void Info(string operation, params  LogItem[] logItems);
		void Warn(string operation, params  LogItem[] logItems);
		void Error(string operation, params LogItem[] logItems);
		void Fatal(string operation, params LogItem[] logItems);
		void Debug(string operation, params LogItem[] logItems);
		void Exception(string operation, string @event, System.Exception exception, params LogItem[] additionalLogItems);

	    LogLevel CurrentLogLevel { get; }
	}
}