using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace StatsigUnity
{
    public class DynamicConfig
    {
        public string ConfigName { get; internal set; }
        public Dictionary<string, JToken> Value { get; internal set; }
        public string RuleID { get; internal set; }
        public List<Dictionary<string, string>> SecondaryExposures { get; internal set; }
        public List<string> ExplicitParameters { get; internal set; }
        public bool IsInLayer { get; internal set; }

        public bool IsUserInExperiment { get; internal set; }
        public bool RulePassed { get; internal set; }

        public DynamicConfig(
            string configName = null,
            Dictionary<string, JToken> value = null,
            string ruleID = null,
            List<Dictionary<string, string>> secondaryExposures = null,
            List<string> explicitParameters = null,
            bool isInLayer = false,
            bool isUserInExperiment = false,
            bool passed = false)
        {
            ConfigName = configName ?? "";
            Value = value ?? new Dictionary<string, JToken>();
            RuleID = ruleID ?? "";
            SecondaryExposures = secondaryExposures ?? new List<Dictionary<string, string>>();
            ExplicitParameters = explicitParameters ?? new List<string>();
            IsInLayer = isInLayer;
            IsUserInExperiment = isUserInExperiment;
            RulePassed = passed;
        }


        public T Get<T>(string key, T defaultValue = default(T))
        {
            if (!Value.TryGetValue(key, out var outVal))
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

            jobj.TryGetValue("rule_id", out var ruleToken);
            jobj.TryGetValue("value", out var valueToken);
            jobj.TryGetValue("secondary_exposures", out var exposuresToken);
            jobj.TryGetValue("explicit_parameters", out var explicitParamsToken);
            jobj.TryGetValue("is_in_layer", out var isInLayerToken);
            jobj.TryGetValue("is_user_in_experiment", out var isUserInExperiment);
            jobj.TryGetValue("passed", out var passedToken);

            try
            {
                return new DynamicConfig(
                    configName,
                    valueToken?.ToObject<Dictionary<string, JToken>>(),
                    ruleToken?.Value<string>(),
                    exposuresToken?.ToObject<List<Dictionary<string, string>>>(),
                    explicitParamsToken?.ToObject<List<string>>(),
                    isInLayerToken?.Value<bool>() ?? false,
                    isUserInExperiment?.Value<bool>() ?? false,
                    passedToken?.Value<bool>() ?? false
                );
            }
            catch
            {
                return null;
            }
        }
    }
}