using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace StatsigUnity
{
    public static class JObjectExtensions
    {

        public static T GetOrDefault<T>(Dictionary<string, JToken> json, string key, T defaultValue)
        {
            json.TryGetValue(key, out var token);
            if (token == null)
            {
                return defaultValue;
            }

            return token.ToObject<T>();
        }
    }
}