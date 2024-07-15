using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StatsigUnity
{
    public static class Statsig
    {
        static StatsigClient _client;

        public static async Task Initialize(string clientKey, StatsigUser user = null, StatsigOptions options = null)
        {
            if (_client != null)
            {
                throw new InvalidOperationException("Cannot re-initialize client.");
            }

            _client = new StatsigClient(clientKey, options);
            await _client.Initialize(user);
        }

        public static async Task Shutdown()
        {
            EnsureInitialized();
            await _client.Shutdown();
        }

        public static async Task Flush()
        {
            EnsureInitialized();
            await _client.Flush();
        }

        public static bool CheckGate(string gateName)
        {
            EnsureInitialized();
            return _client.CheckGate(gateName);
        }

        public static bool CheckGateWithExposureLoggingDisabled(string gateName)
        {
            EnsureInitialized();
            return _client.CheckGateWithExposureLoggingDisabled(gateName);
        }

        public static DynamicConfig GetConfig(string configName)
        {
            EnsureInitialized();
            return _client.GetConfig(configName);
        }

        public static DynamicConfig GetConfigWithExposureLoggingDisabled(string configName)
        {
            EnsureInitialized();
            return _client.GetConfigWithExposureLoggingDisabled(configName);
        }

        public static DynamicConfig GetExperiment(string experimentName)
        {
            EnsureInitialized();
            return _client.GetConfig(experimentName);
        }

        public static DynamicConfig GetExperimentWithExposureLoggingDisabled(string experimentName)
        {
            EnsureInitialized();
            return _client.GetConfigWithExposureLoggingDisabled(experimentName);
        }

        public static Layer GetLayer(string layerName)
        {
            EnsureInitialized();
            return _client.GetLayer(layerName);
        }

        public static Layer GetLayerWithExposureLoggingDisabled(string layerName)
        {
            EnsureInitialized();
            return _client.GetLayerWithExposureLoggingDisabled(layerName);
        }

        public static void LogEvent(
            string eventName,
            string value = null,
            Dictionary<string, string> metadata = null)
        {
            EnsureInitialized();
            _client.LogEvent(eventName, value, metadata);
        }

        public static void LogEvent(
            string eventName,
            int value,
            Dictionary<string, string> metadata = null)
        {
            EnsureInitialized();
            _client.LogEvent(eventName, value, metadata);
        }

        public static void LogEvent(
            string eventName,
            double value,
            Dictionary<string, string> metadata = null)
        {
            EnsureInitialized();
            _client.LogEvent(eventName, value, metadata);
        }

        public static void LogEvent(
            string eventName,
            Dictionary<string, string> metadata)
        {
            EnsureInitialized();
            _client.LogEvent(eventName, metadata);
        }

        public static async Task UpdateUser(StatsigUser user)
        {
            if (user == null)
            {
                throw new InvalidOperationException("user cannot be null.");
            }
            EnsureInitialized();
            await _client.UpdateUser(user);
        }

        static void EnsureInitialized()
        {
            if (_client == null)
            {
                throw new InvalidOperationException("Must call Initialize() first.");
            }
        }
    }
}
