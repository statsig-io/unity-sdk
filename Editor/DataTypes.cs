using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Statsig.UnitySDK
{
    public class UserValues
    {
        [JsonProperty("feature_gates")]
        public IReadOnlyDictionary<string, FeatureGate> FeatureGates { get; set; }
        [JsonProperty("dynamic_configs")]
        public IReadOnlyDictionary<string, DynamicConfig> DynamicConfigs { get; set; }
        [JsonProperty("has_updates")]
        public bool HasUpdates { get; set; }
        [JsonProperty("time")]
        public double Time { get; set; }

        public UserValues()
        {
            FeatureGates = new Dictionary<string, FeatureGate>();
            DynamicConfigs = new Dictionary<string, DynamicConfig>();
            Time = 0;
            HasUpdates = false;
        }
    }

    public class FeatureGate
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("value")]
        public bool Value { get; set; }
        [JsonProperty("rule_id")]
        public string RuleID { get; set; }
        [JsonProperty("secondary_exposures")]
        public List<IReadOnlyDictionary<string, string>> SecondaryExposures { get; set; }

        public FeatureGate(string name = null, bool value = false, string ruleID = null, List<IReadOnlyDictionary<string, string>> secondaryExposures = null)
        {
            Name = name ?? "";
            Value = value;
            RuleID = ruleID ?? "";
            SecondaryExposures = secondaryExposures ?? new List<IReadOnlyDictionary<string, string>>();
        }
    }

    public class DynamicConfig
    {
        [JsonProperty("name")]
        public string ConfigName { get; set; }

        [JsonProperty("value")]
        public IReadOnlyDictionary<string, JToken> Value { get; set; }

        [JsonProperty("rule_id")]
        public string RuleID { get; set; }

        [JsonProperty("secondary_exposures")]
        public List<IReadOnlyDictionary<string, string>> SecondaryExposures { get; set; }

        public DynamicConfig(string configName = null, IReadOnlyDictionary<string, JToken> value = null, string ruleID = null, List<IReadOnlyDictionary<string, string>> secondaryExposures = null)
        {
            ConfigName = configName ?? "";
            Value = value ?? new Dictionary<string, JToken>();
            RuleID = ruleID ?? "";
            SecondaryExposures = secondaryExposures ?? new List<IReadOnlyDictionary<string, string>>();
        }

        public T Get<T>(string key, T defaultValue = default(T))
        {
            JToken outVal = null;
            if (!this.Value.TryGetValue(key, out outVal))
            {
                return defaultValue;
            }

            try
            {
                return outVal.Value<T>();
            }
            catch
            {
                // There are a bunch of different types of exceptions that could
                // be thrown at this point - missing converters, format exception
                // type cast exception, etc.
                return defaultValue;
            }
        }
    }
}