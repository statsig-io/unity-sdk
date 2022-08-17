using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace StatsigUnity
{
    public class DynamicConfig
    {
        [JsonProperty("name")]
        public string ConfigName { get; }

        [JsonProperty("value")]
        public Dictionary<string, JToken> Value { get; }

        [JsonProperty("rule_id")]
        public string RuleID { get; }

        [JsonProperty("secondary_exposures")]
        public List<Dictionary<string, string>> SecondaryExposures { get; }

        static DynamicConfig _defaultConfig;

        public static DynamicConfig Default
        {
            get
            {
                if (_defaultConfig == null)
                {
                    _defaultConfig = new DynamicConfig();
                }
                return _defaultConfig;
            }
        }

        public DynamicConfig(string configName = null, Dictionary<string, JToken> value = null, string ruleID = null, List<Dictionary<string, string>> secondaryExposures = null)
        {
            ConfigName = configName ?? "";
            Value = value ?? new Dictionary<string, JToken>();
            RuleID = ruleID ?? "";
            SecondaryExposures = secondaryExposures ?? new List<Dictionary<string, string>>();
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
                return outVal.ToObject<T>();
            }
            catch
            {
                // There are a bunch of different types of exceptions that could
                // be thrown at this point - missing converters, format exception
                // type cast exception, etc.
                return defaultValue;
            }
        }

        internal static DynamicConfig FromJObject(string configName, JObject jobj)
        {
            if (jobj == null)
            {
                return null;
            }

            JToken ruleToken;
            jobj.TryGetValue("rule_id", out ruleToken);

            JToken valueToken;
            jobj.TryGetValue("value", out valueToken);

            try
            {
                var value = valueToken == null ? null : valueToken.ToObject<Dictionary<string, JToken>>();
                return new DynamicConfig
                (
                    configName,
                    value,
                    ruleToken == null ? null : ruleToken.Value<string>(),
                    jobj.TryGetValue("secondary_exposures", out JToken exposures)
                        ? exposures.ToObject<List<Dictionary<string, string>>>()
                        : new List<Dictionary<string, string>>()
                );
            }
            catch
            {
                return null;
            }
        }
    }
}