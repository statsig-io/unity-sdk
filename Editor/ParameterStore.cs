using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;

namespace StatsigUnity
{
    public class ParameterStore
    {
        public string Name { get; }
        public Dictionary<string, Dictionary<string, JToken>> Parameters { get; }

        public bool DisableExposureLogging { get; } = false;

        private StatsigClient _client;

        public ParameterStore(string name, Dictionary<string, Dictionary<string, JToken>> parameters, StatsigClient client, bool disableExposureLogging = false)
        {

            Name = name;
            Parameters = parameters ?? new Dictionary<string, Dictionary<string, JToken>>();
            _client = client;
            DisableExposureLogging = disableExposureLogging;
        }

        public T Get<T>(string key, T defaultValue = default(T))
        {
            if (!this.Parameters.TryGetValue(key, out var parameter))
            {
                return defaultValue;
            }

            try
            {
                var paramType = parameter["param_type"]?.ToString();
                if (paramType == null)
                {
                    return defaultValue;
                }
                if (defaultValue != null)
                {
                    switch (paramType)
                    {
                        case "boolean":
                            if (!(defaultValue is bool))
                            {
                                return defaultValue;
                            }
                            break;
                        case "number":
                            if (!(defaultValue is int) && !(defaultValue is decimal) && !(defaultValue is long) && !(defaultValue is float) && !(defaultValue is double))
                            {
                                return defaultValue;
                            }
                            break;
                        case "string":
                            if (!(defaultValue is string))
                            {
                                return defaultValue;
                            }
                            break;
                        case "object":
                            var type = defaultValue.GetType();
                            if (!(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                            {
                                return defaultValue;
                            }
                            break;
                        case "array":
                            var isList = defaultValue.GetType().IsGenericType && defaultValue.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
                            var isArray = defaultValue.GetType().IsArray;
                            if (!isList && !isArray)
                            {
                                return defaultValue;
                            }
                            break;
                    }
                }
                return GetValueFromRefType(parameter, defaultValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        private T GetValueFromRefType<T>(Dictionary<string, JToken> parameter, T defaultValue)
        {
            var refType = parameter["ref_type"]?.ToString();
            switch (refType)
            {
                case "static":
                    var staticValue = parameter["value"].ToObject<T>();
                    if (staticValue == null)
                    {
                        return defaultValue;
                    }
                    return staticValue;
                case "gate":
                    var gateName = parameter["gate_name"]?.ToString();
                    var passValue = parameter["pass_value"].ToObject<T>();
                    var failValue = parameter["fail_value"].ToObject<T>();
                    if (gateName == null || passValue == null || failValue == null)
                    {
                        return defaultValue;
                    }
                    var res = DisableExposureLogging ? _client.CheckGateWithExposureLoggingDisabled(gateName) : _client.CheckGate(gateName);
                    return res ? passValue : failValue;
                case "experiment":
                    var experimentName = parameter["experiment_name"]?.ToString();
                    var expParamName = parameter["param_name"]?.ToString();
                    if (experimentName == null || expParamName == null)
                    {
                        return defaultValue;
                    }
                    var experiment = DisableExposureLogging ? _client.GetConfigWithExposureLoggingDisabled(experimentName) : _client.GetConfig(experimentName);
                    if (experiment == null)
                    {
                        return defaultValue;
                    }
                    var experimentValue = experiment.Get(expParamName, defaultValue);
                    if (experimentValue == null)
                    {
                        return defaultValue;
                    }
                    return experimentValue;
                case "dynamic_config":
                    var configName = parameter["config_name"]?.ToString();
                    var configParamName = parameter["param_name"]?.ToString();
                    if (configName == null || configParamName == null)
                    {
                        return defaultValue;
                    }
                    var config = DisableExposureLogging ? _client.GetConfigWithExposureLoggingDisabled(configName) : _client.GetConfig(configName);
                    if (config == null)
                    {
                        return defaultValue;
                    }
                    var configValue = config.Get(configParamName, defaultValue);
                    if (configValue == null)
                    {
                        return defaultValue;
                    }
                    return configValue;
                case "layer":
                    var layerName = parameter["layer_name"]?.ToString();
                    var layerParamName = parameter["param_name"]?.ToString();
                    if (layerName == null || layerParamName == null)
                    {
                        return defaultValue;
                    }
                    var layer = DisableExposureLogging ? _client.GetLayerWithExposureLoggingDisabled(layerName) : _client.GetLayer(layerName);
                    if (layer == null)
                    {
                        return defaultValue;
                    }
                    var layerValue = layer.Get(layerParamName, defaultValue);
                    if (layerValue == null)
                    {
                        return defaultValue;
                    }
                    return layerValue;
                default:
                    return defaultValue;
            }
        }
    }
}
