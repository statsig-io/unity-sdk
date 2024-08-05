using System.Collections.Generic;

namespace StatsigUnity
{
    public enum EnvironmentTier
    {
        Production,
        Development,
        Staging,
    }

    public class StatsigOptions
    {
        public string ApiUrlBase { get; set; }
        public EnvironmentTier? EnvironmentTier { get; set; }

        public int InitializeTimeoutMs { get; set; }

        public int LoggingIntervalMs { get; set; }

        public int LoggingBufferMaxSize { get; set; }

        public bool EnableAsyncCacheWrites { get; set; }

        public StatsigOptions()
        {
            ApiUrlBase = "";
            EnvironmentTier = null;
            InitializeTimeoutMs = Constants.DEFAULT_INITIALIZE_TIMEOUT_MS;
            LoggingIntervalMs = Constants.CLIENT_MAX_LOGGER_WAIT_TIME_IN_MS;
            LoggingBufferMaxSize = Constants.CLIENT_MAX_LOGGER_QUEUE_LENGTH;
            EnableAsyncCacheWrites = false;
        }

        internal Dictionary<string, string> getEnvironmentValues()
        {
            var values = new Dictionary<string, string>();
            if (EnvironmentTier != null)
            {
                values["tier"] = EnvironmentTier.ToString().ToLowerInvariant();
            }
            return values;
        }
    }
}
