using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;

namespace Statsig.UnitySDK
{
    public class StatsigClient : IDisposable
    {
        const string gatesStoreKey = "statsig::featureGates";
        const string configsStoreKey = "statsig::configs";

        readonly StatsigOptions _options;
        internal readonly string _clientKey;
        bool _disposed;
        RequestDispatcher _requestDispatcher;
        EventLogger _eventLogger;
        StatsigUser _user;
        Dictionary<string, string> _statsigMetadata;

        PersistentStore _store;

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
            _eventLogger = new EventLogger(
                _requestDispatcher,
                SDKDetails.GetClientSDKDetails(),
                Constants.CLIENT_MAX_LOGGER_QUEUE_LENGTH,
                Constants.CLIENT_MAX_LOGGER_WAIT_TIME_IN_SEC
            );
        }

        public async Task Initialize(StatsigUser user)
        {
            Debug.Log("initializing");
            if (user == null)
            {
                user = new StatsigUser();
            }

            _user = user;
            _user.statsigEnvironment = _options.StatsigEnvironment.Values;
            _store = new PersistentStore(user.UserID);
            Debug.Log("Started fetching now ");
            var responseJson = await _requestDispatcher.Fetch(
                "initialize",
                new Dictionary<string, object>
                {
                    ["user"] = _user,
                    ["statsigMetadata"] = GetStatsigMetadata(),
                }
            );
            if (responseJson != null)
            {
                _store.updateUserValues(_user.UserID, responseJson);
            }
            // StartCoroutine(_requestDispatcher.Fetch(
            //     "initialize",
            //     new Dictionary<string, object>
            //     {
            //         ["user"] = _user,
            //         ["statsigMetadata"] = GetStatsigMetadata(),
            //     },
            //     (result) =>
            //     {
            //         Debug.Log("result is fetched!!");
            //         Debug.Log(result);
            //         Debug.Log("response JSON is " + result);
            //         try
            //         {
            //             var response = JsonConvert.DeserializeObject<UserValues>(result);
            //             if (response == null)
            //             {
            //                 Debug.Log("returned NULL response");
            //                 return;
            //             }
            //             Debug.Log("returned response");
            //             _values = response;
            //         }
            //         catch (Exception e)
            //         {
            //             Debug.Log(e.Message);
            //         }
            //     }
            // ));
        }

        public void Shutdown()
        {
            _eventLogger.Shutdown();
            ((IDisposable)this).Dispose();
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
            _eventLogger.Enqueue(EventLog.CreateGateExposureLog(_user, gateName, gate.Value, gate.RuleID, gate.SecondaryExposures));
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
            _eventLogger.Enqueue(EventLog.CreateConfigExposureLog(_user, configName, config.RuleID, config.SecondaryExposures));
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

        void IDisposable.Dispose()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("StatsigClient");
            }

            _eventLogger.ForceFlush();
            _disposed = true;
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
                string systemName = "unknown";
                // if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                // {
                //     systemName = "Mac OS";
                // }
                // else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                // {
                //     systemName = "Windows";
                // }
                // else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                // {
                //     systemName = "Linux";
                // }
                _statsigMetadata = new Dictionary<string, string>
                {
                    ["sessionID"] = Guid.NewGuid().ToString(),
                    // ["stableID"] = PersistentStore.StableID,
                    // ["locale"] = CultureInfo.CurrentUICulture.Name,
                    // ["appVersion"] = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                    // ["systemVersion"] = Environment.OSVersion.Version.ToString(),
                    // ["systemName"] = systemName,
                    ["sdkType"] = SDKDetails.GetClientSDKDetails().SDKType,
                    ["sdkVersion"] = SDKDetails.GetClientSDKDetails().SDKVersion,
                };
            }
            return _statsigMetadata;
        }

        #endregion
    }
}
