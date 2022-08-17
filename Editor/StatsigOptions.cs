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

        public StatsigOptions()
        {
            ApiUrlBase = Constants.DEFAULT_API_URL_BASE;
            EnvironmentTier = null;
            InitializeTimeoutMs = Constants.DEFAULT_INITIALIZE_TIMEOUT_MS;
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
