using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace StatsigUnity
{
    public class ParamStoreSpec
    {
        public string Name { get; }
        public Dictionary<string, Dictionary<string, JToken>> Parameters { get; }

        public ParamStoreSpec(string name, Dictionary<string, Dictionary<string, JToken>> parameters)
        {
            Name = name;
            Parameters = parameters ?? new Dictionary<string, Dictionary<string, JToken>>();
        }

        internal static ParamStoreSpec FromJObject(string name, JObject jobj)
        {
            if (jobj == null)
            {
                return null;
            }

            try
            {
                return new ParamStoreSpec
                (
                    name,
                    jobj.ToObject<Dictionary<string, Dictionary<string, JToken>>>()
                );
            }
            catch
            {
                return null;
            }
        }
    }
}
