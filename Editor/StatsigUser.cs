using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace StatsigUnity
{
    public class StatsigUser
    {
        internal Dictionary<string, string> properties;
        internal Dictionary<string, object> customProperties;
        internal Dictionary<string, object> privateAttributes;
        internal Dictionary<string, string> customIDs;

        public string UserID
        {
            get
            {
                return properties.TryGetValue("userID", out string value) ? value : null;
            }
            set
            {
                properties["userID"] = value;
            }
        }
        public string Email
        {
            get
            {
                return properties.TryGetValue("email", out string value) ? value : null;
            }
            set
            {
                properties["email"] = value;
            }
        }
        public string IPAddress
        {
            get
            {
                return properties.TryGetValue("ip", out string value) ? value : null;
            }
            set
            {
                properties["ip"] = value;
            }
        }
        public string UserAgent
        {
            get
            {
                return properties.TryGetValue("userAgent", out string value) ? value : null;
            }
            set
            {
                properties["userAgent"] = value;
            }
        }
        public string Country
        {
            get
            {
                return properties.TryGetValue("country", out string value) ? value : null;
            }
            set
            {
                properties["country"] = value;
            }
        }
        public string Locale
        {
            get
            {
                return properties.TryGetValue("locale", out string value) ? value : null;
            }
            set
            {
                properties["locale"] = value;
            }
        }
        public string AppVersion
        {
            get
            {
                return properties.TryGetValue("appVersion", out string value) ? value : null;
            }
            set
            {
                properties["appVersion"] = value;
            }
        }
        public Dictionary<string, object> CustomProperties => customProperties;
        public Dictionary<string, object> PrivateAttributes => privateAttributes;
        internal Dictionary<string, string> statsigEnvironment;
        public Dictionary<string, string> CustomIDs => customIDs;

        public StatsigUser()
        {
            properties = new Dictionary<string, string>();
            customProperties = new Dictionary<string, object>();
            privateAttributes = new Dictionary<string, object>();
            statsigEnvironment = new Dictionary<string, string>();
            customIDs = new Dictionary<string, string>();
        }

        public void AddCustomProperty(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be empty.", "key");
            }
            customProperties[key] = value;
        }

        public void AddPrivateAttribute(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be empty.", "key");
            }
            privateAttributes[key] = value;
        }

        public void AddCustomID(string idType, string value)
        {
            if (string.IsNullOrWhiteSpace(idType))
            {
                throw new ArgumentException("idType cannot be empty.", "idType");
            }
            customIDs[idType] = value;
        }

        internal StatsigUser GetCopyForLogging()
        {
            var copy = new StatsigUser
            {
                UserID = UserID,
                Email = Email,
                IPAddress = IPAddress,
                UserAgent = UserAgent,
                Country = Country,
                Locale = Locale,
                AppVersion = AppVersion,
                customIDs = customIDs,
                customProperties = customProperties,
                statsigEnvironment = statsigEnvironment,
                // Do NOT add private attributes here
            };
            return copy;
        }

        internal Dictionary<string, object> ToDictionary(bool includePrivateAttributes)
        {
            var result = new Dictionary<string, object>
            {
                { "userID", UserID },
                { "email", Email },
                { "ip", IPAddress },
                { "userAgent", UserAgent },
                { "country", Country },
                { "locale", Locale },
                { "appVersion", AppVersion },
                { "custom", CustomProperties },
                { "customIDs", CustomIDs },
                { "statsigEnvironment", statsigEnvironment },
            };

            if (includePrivateAttributes)
            {
                result["privateAttributes"] = PrivateAttributes;
            }
            return result;
        }

        internal string ToHash()
        {
            var result = new Dictionary<string, object>
            {
                { "userID", UserID },
                { "email", Email },
                { "ip", IPAddress },
                { "userAgent", UserAgent },
                { "country", Country },
                { "locale", Locale },
                { "appVersion", AppVersion },
                { "custom", CustomProperties },
                { "customIDs", CustomIDs },
                { "statsigEnvironment", statsigEnvironment },
                { "privateAttributes", PrivateAttributes },
            };
            string jsonResult = JsonConvert.SerializeObject(result);
            int hash = 0;
            for (int i = 0; i < jsonResult.Length; i++)
            {
                var character = jsonResult[i];
                hash = (hash << 5) - hash + character;
                hash = hash & hash; // Convert to 32bit integer
            }
            return hash.ToString();
        }
    }
}
