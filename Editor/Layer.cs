using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace StatsigUnity
{
    public class Layer
    {
        [JsonProperty("name")]
        public string Name { get; }

        [JsonProperty("rule_id")]
        public string RuleID { get; }

        [JsonProperty("allocated_experiment")]
        public string AllocatedExperiment { get; }

        [JsonProperty("secondary_exposures")]
        public List<IReadOnlyDictionary<string, string>> SecondaryExposures { get; }

        [JsonProperty("value")]
        private IReadOnlyDictionary<string, JToken> Value { get; }

        static Layer _defaultLayer;

        public static Layer Default
        {
            get
            {
                if (_defaultLayer == null)
                {
                    _defaultLayer = new Layer();
                }
                return _defaultLayer;
            }
        }

        public Layer(string name = null, IReadOnlyDictionary<string, JToken> value = null, string ruleID = null, string allocatedExperiment = null, List<IReadOnlyDictionary<string, string>> secondaryExposures = null)
        {
            Name = name ?? "";
            Value = value ?? new Dictionary<string, JToken>();
            RuleID = ruleID ?? "";
            AllocatedExperiment = allocatedExperiment ?? "";
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

        internal static Layer FromJObject(string configName, JObject jobj)
        {
            if (jobj == null)
            {
                return null;
            }

            JToken ruleToken;
            jobj.TryGetValue("rule_id", out ruleToken);

            JToken valueToken;
            jobj.TryGetValue("value", out valueToken);

            JToken allocatedExperimentToken;
            jobj.TryGetValue("allocated_experiment_name", out allocatedExperimentToken);

            try
            {
                var value = valueToken == null ? null : valueToken.ToObject<Dictionary<string, JToken>>();
                return new Layer
                (
                    configName,
                    value,
                    ruleToken == null ? null : ruleToken.Value<string>(),
                    allocatedExperimentToken == null ? null : allocatedExperimentToken.Value<string>(),
                    jobj.TryGetValue("secondary_exposures", out JToken exposures)
                        ? exposures.ToObject<List<IReadOnlyDictionary<string, string>>>()
                        : new List<IReadOnlyDictionary<string, string>>()
                );
            }
            catch
            {
                return null;
            }
        }
    }
}