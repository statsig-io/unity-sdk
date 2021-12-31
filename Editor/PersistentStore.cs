using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;

namespace Statsig.UnitySDK
{
    internal class PersistentStore
    {
        const string stableIDKey = "statsig::stableID";
        const string userValuesKeyPrefix = "statsig::userValues-";
        const string logEventsKey = "statsig::logEvents"; // TODO: use this to cache events

        internal string stableID;
        private UserValues _values = new UserValues();

        internal PersistentStore(string userID)
        {
            stableID = PlayerPrefs.GetString(stableIDKey, null);
            if (stableID == null)
            {
                stableID = Guid.NewGuid().ToString();
                PlayerPrefs.SetString(stableIDKey, stableID);
            }

            var cachedValues = PlayerPrefs.GetString(getUserValueKey(userID), null);
            if (cachedValues != null)
            {
                try
                {
                    var values = JsonConvert.DeserializeObject<UserValues>(cachedValues);
                    if (values == null)
                    {
                        PlayerPrefs.DeleteKey(getUserValueKey(userID));
                    }
                    else
                    {
                        _values = values;
                    }
                }
                catch (Exception)
                {
                    PlayerPrefs.DeleteKey(getUserValueKey(userID));
                }
            }

            PlayerPrefs.Save();
        }

#nullable enable
        internal FeatureGate? getGate(string gateName)
        {
            FeatureGate gate;
            if (!_values.FeatureGates.TryGetValue(gateName, out gate))
            {
                return null;
            }
            return gate;
        }

#nullable enable
        internal DynamicConfig? getConfig(string configName)
        {
            DynamicConfig config;
            if (!_values.DynamicConfigs.TryGetValue(configName, out config))
            {
                return null;
            }
            return config;
        }

        internal void updateUserValues(string userID, string values)
        {
            try
            {
                var parsed = JsonConvert.DeserializeObject<UserValues>(values);
                if (parsed != null)
                {
                    _values = parsed;
                    PlayerPrefs.SetString(getUserValueKey(userID), values);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception)
            {
            }
        }

        private string getUserValueKey(string userID)
        {
            return userValuesKeyPrefix + userID ?? "";
        }
    }
}
