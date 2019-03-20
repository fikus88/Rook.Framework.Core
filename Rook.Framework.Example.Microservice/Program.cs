using System;
using System.Threading;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Services;
using Rook.Framework.Core.StructureMap;

namespace Rook.Framework.Example.Microservice
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // ---------------------------------
            //  STRUCTURE MAP CONFIG // If you're using Structuremap - i.e. UseStructureMap=true in config.json
            // ---------------------------------
            var container = Bootstrapper.Init();
            
            var service = container.GetInstance<IService>();

            // It's also possible to use these utility methods so we can see what the container has scanned/registered
            var whatDidIScan = container.WhatDidIScan();
            var whatDoIHave = container.WhatDoIHave();


            // ---------------------------------
            
            // ---------------------------------
            //  LEGACY CONFIG // If you're not using Structuremap - you probably should - but you need to use this
            // ---------------------------------
            // Container.Map<IContainerFacade>(new ContainerFacade(null, Container.GetInstance<IConfigurationManager>()));
            // var service = Container.GetInstance<IService>();
            // ---------------------------------

            Thread.CurrentThread.Name = $"{ServiceInfo.Name} Main Thread";

            service.Start();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => service.Stop();

            Thread.CurrentThread.IsBackground = true;

            while (true)
                Thread.Sleep(int.MaxValue);
        }
    }
}
