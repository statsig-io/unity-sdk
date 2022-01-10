using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;

namespace StatsigUnity
{
    public class StatsigClient
    {
        const string gatesStoreKey = "statsig::featureGates";
        const string configsStoreKey = "statsig::configs";

        readonly StatsigOptions _options;
        internal readonly string _clientKey;
        RequestDispatcher _requestDispatcher;
        EventLogger _eventLogger;
        StatsigUser _user;
        Dictionary<string, string> _statsigMetadata;

        PersistentStore _store;

        GameObject _dummyGameObject;

        public StatsigClient(string clientKey, StatsigOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(clientKey))
            {
                throw new ArgumentException("clientKey cannot be empty.", "clientKey");
            }
            if (!clientKey.StartsWith("client-") && !clientKey.StartsWith("test-"))
            {
                throw new ArgumentException("Invalid key provided. Please check your Statsig console to get the right server key.", "serverSecret");
            }
            if (options == null)
            {
                options = new StatsigOptions();
            }
            _clientKey = clientKey;
            _options = options;
            _requestDispatcher = new RequestDispatcher(_clientKey, _options.ApiUrlBase);
            _dummyGameObject = new GameObject("DetectPlayer");
            _eventLogger = _dummyGameObject.AddComponent<EventLogger>();
            _eventLogger.Init(_requestDispatcher);
        }

        public async Task Initialize(StatsigUser user)
        {
            if (user == null)
            {
                user = new StatsigUser();
            }

            _user = user;
            _user.statsigEnvironment = _options.getEnvironmentValues();
            _store = new PersistentStore(user.UserID);

            Task[] tasks;
            var requestTask = _requestDispatcher.Fetch(
                "initialize",
                new Dictionary<string, object>
                {
                    ["user"] = _user,
                    ["statsigMetadata"] = GetStatsigMetadata(),
                }, 5)
                .ContinueWith(t =>
                {
                    var responseJson = t.Result;
                    if (responseJson != null)
                    {
                        _store.updateUserValues(_user.UserID, responseJson);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext())
            ;
            if (_options.InitializeTimeoutMs > 0)
            {
                var timeoutTask = Task.Delay(_options.InitializeTimeoutMs);
                tasks = new Task[] { requestTask, timeoutTask };
            }
            else
            {
                tasks = new Task[] { requestTask };
            }

            var completed = await Task.WhenAny(tasks);
            await completed;
        }

        public bool CheckGate(string gateName)
        {
            var hashedName = GetNameHash(gateName);
            var gate = _store.getGate(hashedName);
            if (gate == null)
            {
                gate = _store.getGate(gateName);
                if (gate == null)
                {
                    gate = new FeatureGate(gateName, false, "");
                }
            }
            _eventLogger.LogGateExposure(_user, gateName, gate.Value, gate.RuleID, gate.SecondaryExposures);
            return gate.Value;
        }

        public DynamicConfig GetConfig(string configName)
        {
            var hashedName = GetNameHash(configName);
            var config = _store.getConfig(hashedName);
            if (config == null)
            {
                config = _store.getConfig(configName);
                if (config == null)
                {
                    config = new DynamicConfig(configName);
                }
            }
            _eventLogger.LogConfigExposure(_user, configName, config.RuleID, config.SecondaryExposures);
            return config;
        }

        public async Task UpdateUser(StatsigUser newUser)
        {
            _statsigMetadata = null;
            await Initialize(newUser);
        }

        public void LogEvent(
            string eventName,
            string value = null,
            IReadOnlyDictionary<string, string> metadata = null)
        {
            if (value != null && value.Length > Constants.MAX_SCALAR_LENGTH)
            {
                value = value.Substring(0, Constants.MAX_SCALAR_LENGTH);
            }

            LogEventHelper(eventName, value, metadata);
        }

        public void LogEvent(
            string eventName,
            int value,
            IReadOnlyDictionary<string, string> metadata = null)
        {
            LogEventHelper(eventName, value, metadata);
        }

        public void LogEvent(
            string eventName,
            double value,
            IReadOnlyDictionary<string, string> metadata = null)
        {
            LogEventHelper(eventName, value, metadata);
        }

        public void LogEvent(
            string eventName,
            IReadOnlyDictionary<string, string> metadata)
        {
            LogEventHelper(eventName, null, metadata);
        }

        public async Task Shutdown()
        {
            UnityEngine.Object.Destroy(_dummyGameObject);
            await _eventLogger.Shutdown();
        }

        #region Private helpers

        void LogEventHelper(
            string eventName,
            object value,
            IReadOnlyDictionary<string, string> metadata = null)
        {
            if (eventName == null)
            {
                return;
            }

            if (eventName.Length > Constants.MAX_SCALAR_LENGTH)
            {
                eventName = eventName.Substring(0, Constants.MAX_SCALAR_LENGTH);
            }

            var eventLog = new EventLog
            {
                EventName = eventName,
                Value = value,
                User = _user,
                Metadata = EventLog.TrimMetadataAsNeeded(metadata),
            };

            _eventLogger.Enqueue(eventLog);
        }

        string GetNameHash(string name)
        {
            using (var sha = SHA256.Create())
            {
                var buffer = sha.ComputeHash(Encoding.UTF8.GetBytes(name));
                return Convert.ToBase64String(buffer);
            }
        }

        IReadOnlyDictionary<string, string> GetStatsigMetadata()
        {
            if (_statsigMetadata == null)
            {
                _statsigMetadata = new Dictionary<string, string>
                {
                    ["sessionID"] = Guid.NewGuid().ToString(),
                    ["stableID"] = _store.stableID,
                    ["language"] = Application.systemLanguage.ToString(),
                    ["platform"] = Application.platform.ToString(),
                    ["appVersion"] = Application.version,
                    ["operatingSystem"] = SystemInfo.operatingSystem,
                    ["deviceModel"] = SystemInfo.deviceModel,
                    ["batteryLevel"] = SystemInfo.batteryLevel.ToString(),
                    ["sdkType"] = SDKDetails.SDKType,
                    ["sdkVersion"] = SDKDetails.SDKVersion,
                };
            }
            return _statsigMetadata;
        }

        #endregion
    }
}