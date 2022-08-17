using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace StatsigUnity
{
    public class EventLog
    {
        private StatsigUser _user;

        public string EventName { get; set; }
        public StatsigUser User
        {
            get => _user;
            set
            {
                // C# pass by reference so we need to make a copy of user that does NOT have private attributes
                _user = value.GetCopyForLogging();
            }
        }
        public Dictionary<string, string> Metadata { get; set; }
        public object Value { get; set; }
        public double Time { get; } = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
        public List<Dictionary<string, string>> SecondaryExposures { get; set; }

        internal bool IsErrorLog { get; set; }
        internal string ErrorKey { get; set; }

        public EventLog()
        {
        }

        internal static EventLog CreateErrorLog(string eventName, string errorMessage = null)
        {
            if (errorMessage == null)
            {
                errorMessage = eventName;
            }

            return new EventLog
            {
                EventName = eventName,
                Metadata = new Dictionary<string, string>
                {
                    ["error"] = errorMessage
                },
                IsErrorLog = true,
                ErrorKey = errorMessage,
            };
        }

        internal static Dictionary<string, string> TrimMetadataAsNeeded(Dictionary<string, string> metadata = null)
        {
            if (metadata == null)
            {
                return null;
            }

            int totalLength = metadata.Sum((kv) => kv.Key.Length + (kv.Value == null ? 0 : kv.Value.Length));
            if (totalLength > Constants.MAX_METADATA_LENGTH)
            {
                Debug.WriteLine("Metadata in LogEvent is too big, dropping it.", "warning");
                return null;
            }

            return metadata;
        }

        internal Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                { "eventName", EventName },
                { "user", User.ToDictionary(false) },
                { "metadata", Metadata },
                { "value", Value },
                { "time", Time },
                { "secondaryExposures", SecondaryExposures },
                
            };
        }
    }
}
