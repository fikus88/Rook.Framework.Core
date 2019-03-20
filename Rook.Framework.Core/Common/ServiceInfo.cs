using System.IO;
using System.Reflection;

namespace Rook.Framework.Core.Common
{
    public static class ServiceInfo
    {
        private static string name;
        private static string majorVersion;
        private static string version;
        private static string microServiceCoreVersion;
        private static string queueName;

        private static string GetServiceName()
        {
            string location = Assembly.GetEntryAssembly()?.Location ?? "Unknown";
            return Path.GetFileName(Path.GetFileNameWithoutExtension(location));
        }
        private static string GetMajorVersion()
        {
            return Assembly.GetEntryAssembly().GetName().Version.Major.ToString();
        }

        private static string GetServiceVersion()
        {
            return Assembly.GetEntryAssembly().GetName().Version.ToString();
        }

        private static string GetMicroServiceCoreVersion()
        {
            // Assumes this class is in Rook.Framework.Core
            return typeof(ServiceInfo).Assembly.GetName().Version.ToString();
        }


        private static string GetQueueName()
        {
            return $"{GetServiceName()}{GetMajorVersion()}";
        }
        /// <summary>
        /// Returns the filename of the executable without its extension.
        /// </summary>
        public static string Name => name ?? (name = GetServiceName());
        public static string MajorVersion => majorVersion ?? (majorVersion = GetMajorVersion());
        public static string Version => version ?? (version = GetServiceVersion());
        public static string QueueName => queueName ?? (queueName = GetQueueName());
        public static string MicroServiceCoreVersion => microServiceCoreVersion ?? (microServiceCoreVersion = GetMicroServiceCoreVersion());


    }
}
