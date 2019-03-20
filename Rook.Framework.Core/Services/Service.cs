using System;
using System.Diagnostics;
using System.Linq;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.StructureMap;

namespace Rook.Framework.Core.Services
{
    public sealed class Service : IService
    {
        private readonly ILogger _logger;
        private readonly IContainerFacade _container;

        public Service(ILogger logger, IContainerFacade container)
        {
            _logger = logger;
            _container = container;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Stop();
            
            Console.CancelKeyPress += (s, e) => Stop();
        }

        /// <summary>
        /// Initialises the service
        /// </summary>
        /// <returns></returns>
        public void Start()
        {
            var startables = _container.GetAllInstances<IStartable>()
                .OrderBy(x => (byte) x.StartupPriority)
                .ToList();

            foreach (var startable in startables)
            {
                startable.Start();
            }

            _logger.Info($"{nameof(Service)}.{nameof(Start)}",
                new LogItem("Event", "Service Started"),
                new LogItem("Name", ServiceInfo.Name));
        }

        /// <summary>
        /// Shuts down the service
        /// </summary>
        /// <returns></returns>
        public void Stop()
        {
            _container.GetAllInstances<IStartStoppable>()
                .OrderByDescending(x => (byte) x.StartupPriority)
                .ToList().ForEach(x => x.Stop());

            _logger.Info($"{nameof(Service)}.{nameof(Stop)}",
                new LogItem("Event", "Service Stopped"),
                new LogItem("Name", ServiceInfo.Name));
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                _logger?.Warn("UnhandledException",
                    new LogItem("Exception", exception.Message),
                    new LogItem("StackTrace", exception.StackTrace));
            }            
            
            Process.GetCurrentProcess().Close();
        }
    }
}
