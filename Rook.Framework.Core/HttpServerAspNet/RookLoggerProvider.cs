using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using StructureMap;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Rook.Framework.Core.HttpServerAspNet
{
	public class RookLoggerProvider : ILoggerProvider
	{
		private readonly IContainer _container;
		private readonly ConcurrentDictionary<string, RookLogger> _loggers = new ConcurrentDictionary<string, RookLogger>();

		public RookLoggerProvider(IContainer container)
		{
			_container = container;
		}

		public void Dispose()
		{
			_loggers.Clear();
		}

		public ILogger CreateLogger(string categoryName)
		{
			var logger = _container.GetInstance<Common.ILogger>();
			return _loggers.GetOrAdd(categoryName, name => new RookLogger(logger, categoryName));
		}
	}
}
