using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace StatsigUnity
{
    public class RequestDispatcher
    {
        const int backoffMultiplier = 2;
        private static readonly HashSet<int> retryCodes = new HashSet<int> { 408, 500, 502, 503, 504, 522, 524, 599 };
        public string Key { get; }
        public string ApiBaseUrl { get; }
        public string LoggingApiBaseUrl { get; }

        private readonly Func<int, Task> _retryDelay = Task.Delay;
        private readonly string _initializationUrl;
        private readonly string _eventLoggingUrl;
        private readonly object _headersLock = new object();
        private readonly Dictionary<string, string> _requestHeaders =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal RequestDispatcher(string key, StatsigOptions options, Func<int, Task> retryDelay = null)
            : this(key, options.ApiUrlBase, options.ApiUrlBase)
        {
            _retryDelay = retryDelay ?? Task.Delay;
            _initializationUrl = ValidateEndpointUrl(options.InitializationUrl, "InitializationUrl");
            _eventLoggingUrl = ValidateEndpointUrl(options.EventLoggingUrl, "EventLoggingUrl");
            if (options.RequestHeaders != null)
            {
                foreach (var header in options.RequestHeaders)
                {
                    SetRequestHeader(header.Key, header.Value);
                }
            }
        }

        private static string ValidateEndpointUrl(string url, string parameterName)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }
            Uri parsed;
            if (!Uri.TryCreate(url, UriKind.Absolute, out parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(parsed.Fragment) || !string.IsNullOrEmpty(parsed.UserInfo))
            {
                throw new ArgumentException("Expected an absolute HTTP(S) URL without credentials or a fragment.", parameterName);
            }
            return url;
        }

        private static void ValidateHeaderName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Header name cannot be empty.", "name");
            }
            const string punctuation = "!#$%&'*+-.^_`|~";
            foreach (var ch in name)
            {
                if (!((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') ||
                    (ch >= '0' && ch <= '9') || punctuation.IndexOf(ch) >= 0))
                {
                    throw new ArgumentException("Header name must be an HTTP token.", "name");
                }
            }
            if (string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "STATSIG-API-KEY", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "STATSIG-CLIENT-TIME", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("This header is managed by the SDK.", "name");
            }
        }

        internal void SetRequestHeader(string name, string value)
        {
            ValidateHeaderName(name);
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Header value cannot be empty; use RemoveRequestHeader instead.", "value");
            }
            foreach (var ch in value)
            {
                if (ch < 32 || ch > 126)
                {
                    throw new ArgumentException("Header value must contain printable ASCII characters only.", "value");
                }
            }
            lock (_headersLock)
            {
                _requestHeaders[name] = value;
            }
        }

        internal void RemoveRequestHeader(string name)
        {
            ValidateHeaderName(name);
            lock (_headersLock)
            {
                _requestHeaders.Remove(name);
            }
        }

        private Dictionary<string, string> GetRequestHeaders()
        {
            lock (_headersLock)
            {
                return new Dictionary<string, string>(_requestHeaders, StringComparer.OrdinalIgnoreCase);
            }
        }

        public RequestDispatcher(string key, string apiBaseUrl = null, string loggingApiBaseUrl = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be empty.", "key");
            }

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                apiBaseUrl = Constants.DEFAULT_API_URL_BASE;
            }

            if (string.IsNullOrWhiteSpace(loggingApiBaseUrl))
            {
                loggingApiBaseUrl = Constants.DEFAULT_LOGGING_API_URL_BASE;
            }

            Key = key;
            ApiBaseUrl = apiBaseUrl;
            LoggingApiBaseUrl = loggingApiBaseUrl;
        }

        public async Task<string> Fetch(
            string endpoint,
            Dictionary<string, object> body,
            int retries = 0,
            int backoff = 1)
        {
            try
            {
                var jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                var isInitialization = endpoint.StartsWith("initialize");
                var baseUrl = isInitialization ? ApiBaseUrl : LoggingApiBaseUrl;
                var url = (isInitialization ? _initializationUrl : _eventLoggingUrl) ??
                    (baseUrl.EndsWith("/") ? baseUrl + endpoint : baseUrl + "/" + endpoint);
                var headers = GetRequestHeaders();
                var json = JsonConvert.SerializeObject(body, Formatting.None, jsonSettings);

                using (var request = new UnityWebRequest(url, "POST"))
                {
                    var bytes = new System.Text.UTF8Encoding().GetBytes(json);
                    if (request.uploadHandler != null)
                    {
                        request.uploadHandler.Dispose();
                    }
                    request.uploadHandler = new UploadHandlerRaw(bytes);
                    if (request.downloadHandler != null)
                    {
                        request.downloadHandler.Dispose();
                    }
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = 10;

                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("STATSIG-API-KEY", Key);
                    request.SetRequestHeader("STATSIG-CLIENT-TIME",
                        (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds.ToString());

                    // Do not forward proxy credentials through transport-managed redirects.
                    if (headers.Count > 0)
                    {
                        request.redirectLimit = 0;
                    }
                    foreach (var header in headers)
                    {
                        request.SetRequestHeader(header.Key, header.Value);
                    }

                    request.SendWebRequest();
                    while (!request.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.responseCode == 200 || request.responseCode == 201)
                    {
                        var result = request.downloadHandler.text;
                        return result;
                    }

                    if (retries > 0 && retryCodes.Contains((int)request.responseCode))
                    {
                        return await retry(endpoint, body, retries, backoff);
                    }
                }
            }
            catch (Exception e)
            {
            }

            return null;
        }

        private async Task<string> retry(
            string endpoint,
            Dictionary<string, object> body,
            int retries = 0,
            int backoff = 1)
        {
            await _retryDelay(1000 * backoff);
            return await Fetch(endpoint, body, retries - 1, backoff * backoffMultiplier);
        }
    }
}