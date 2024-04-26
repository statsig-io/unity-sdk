namespace StatsigUnity
{
    internal static class Constants
    {
        public static string DEFAULT_LOGGING_API_URL_BASE = "https://featuregates.org/v1";
        public static string DEFAULT_API_URL_BASE = "https://statsigapi.net/v1";
        public static int DEFAULT_INITIALIZE_TIMEOUT_MS = 5000;
        public static int MAX_SCALAR_LENGTH = 64;
        public static int MAX_METADATA_LENGTH = 1024;
        public static int CLIENT_MAX_LOGGER_QUEUE_LENGTH = 100;
        public static int CLIENT_MAX_LOGGER_WAIT_TIME_IN_MS = 60000;
        public static string GATE_EXPOSURE_EVENT = "statsig::gate_exposure";
        public static string CONFIG_EXPOSURE_EVENT = "statsig::config_exposure";
        public static string LAYER_EXPOSURE_EVENT = "statsig::layer_exposure";
    }
}
