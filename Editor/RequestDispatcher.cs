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
                var baseUrl = endpoint.StartsWith("log") ? LoggingApiBaseUrl : ApiBaseUrl;
                var url = baseUrl.EndsWith("/") ? baseUrl + endpoint : baseUrl + "/" + endpoint;
                var json = JsonConvert.SerializeObject(body, Formatting.None, jsonSettings);

                using (var request = UnityWebRequest.Post(url, json))
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
            await Task.Delay(1000 * backoff);
            return await Fetch(endpoint, body, retries - 1, backoff * backoffMultiplier);
        }
    }
}