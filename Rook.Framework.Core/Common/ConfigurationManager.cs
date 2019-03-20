using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Rook.Framework.Core.Common
{
    internal sealed class ConfigurationManager : IConfigurationManager
    {
        private IDictionary<string, string> _appSettings;
        private IDictionary<string, string> _environment;

        public IDictionary<string, string> AppSettings => _appSettings ?? (_appSettings = JsonConvert.DeserializeObject<AutoDictionary<string, string>>(File.ReadAllText("config.json")));

        public IDictionary<string, string> Environment => _environment ?? (_environment = new AutoDictionary<string, string>(System.Environment.GetEnvironmentVariables()));

        public T Get<T>(string key)
        {
            try
            {
                var value = GetValueFor(key);
                if (value != null) return (T) Convert.ChangeType(value, typeof(T));
            }
            catch (OverflowException)
            {
                throw new ConfigurationException(key, $"Value outside range of {typeof(T).Name}");
            }

            throw new ConfigurationException(key, "Must specify in environment variables or config.json");
        }

        public T Get<T>(string key, T fallback)
        {
            try
            {
                Type t = typeof(T);
                t = Nullable.GetUnderlyingType(t) ?? t;

                var value = GetValueFor(key);
                if (value != null) return (T) Convert.ChangeType(value, t);
            }
            catch (OverflowException)
            {
                throw new ConfigurationException(key, $"Value outside range of {typeof(T).Name}");
            }

            return fallback;
        }

        private string GetValueFor(string key)
        {
            // Env variables setup in TFS are always uppercase
            return Environment[key.ToUpper()] ?? AppSettings[key];
        }

        class ConfigurationException : Exception
        {
            public ConfigurationException(string key, string message)
                :base($"{key}: {message}")
            {
            }
        }
    }

}