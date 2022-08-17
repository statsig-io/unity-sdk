using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace StatsigUnity
{
   public class Layer
    {
        public string Name { get; }

        public string RuleID { get; }

        internal Dictionary<string, JToken> Value { get; }

        internal List<Dictionary<string, string>> SecondaryExposures;

        internal List<Dictionary<string, string>> UndelegatedSecondaryExposures;

        internal List<string> ExplicitParameters;

        internal string AllocatedExperimentName;

        internal Action<Layer, string> OnExposure;

        static Layer _default;

        public static Layer Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new Layer();
                }
                return _default;
            }
        }

        public Layer(string name = null, Dictionary<string, JToken> value = null, string ruleID = null, Action<Layer, string> onExposure = null)
        {
            Name = name ?? "";
            Value = value ?? new Dictionary<string, JToken>();
            RuleID = ruleID ?? "";
            OnExposure = onExposure ?? delegate { };
        }

        public T Get<T>(string key, T defaultValue = default(T))
        {
            JToken outVal;
            if (!this.Value.TryGetValue(key, out outVal))
            {
                return defaultValue;
            }

            try
            {
                var result = outVal.ToObject<T>();
                OnExposure(this, key);
                return result;
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


            try
            {
                var layer = new Layer
                (
                    configName,
                    GetFromJSON<Dictionary<string, JToken>>(jobj, "value", null),
                    GetFromJSON<string>(jobj, "rule_id", null)
                )
                {
                    AllocatedExperimentName = GetFromJSON(jobj, "allocated_experiment_name", ""),
                    SecondaryExposures = GetFromJSON(jobj, "secondary_exposures", new List<Dictionary<string, string>>()),
                    UndelegatedSecondaryExposures = GetFromJSON(jobj, "undelegated_secondary_exposures", new List<Dictionary<string, string>>()),
                    ExplicitParameters = GetFromJSON(jobj, "explicit_parameters", new List<string>())
                };

                return layer;
            }
            catch
            {
                // Failed to parse config.  TODO: Log this
                return null;
            }
        }

        private static T GetFromJSON<T>(JObject json, string key, T defaultValue)
        {
            JToken token;
            json.TryGetValue(key, out token);
            return token == null ? defaultValue : token.ToObject<T>();
        }
    }

}