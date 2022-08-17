using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace StatsigUnity
{
    public class RequestDispatcher
    {
        const int backoffMultiplier = 2;
        private static readonly HashSet<int> retryCodes = new HashSet<int> { 408, 500, 502, 503, 504, 522, 524, 599 };
        public string Key { get; }
        public string ApiBaseUrl { get; }
        private HttpClient _client;
        public RequestDispatcher(string key, string apiBaseUrl = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be empty.", "key");
            }
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                apiBaseUrl = Constants.DEFAULT_API_URL_BASE;
            }

            Key = key;
            ApiBaseUrl = apiBaseUrl;

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("STATSIG-API-KEY", Key);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> Fetch(
            string endpoint,
            Dictionary<string, object> body,
            int retries = 0,
            int backoff = 1)
        {
            try
            {
                _client.DefaultRequestHeaders.Add("STATSIG-CLIENT-TIME",
                    (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds.ToString());

                var jsonSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                var url = ApiBaseUrl.EndsWith("/") ? ApiBaseUrl + endpoint : ApiBaseUrl + "/" + endpoint;
                var json = JsonConvert.SerializeObject(body, Formatting.None, jsonSettings);
                var data = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync(url, data);
                if (response == null)
                {
                    return null;
                }
                if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    return result;
                }
                else if (retries > 0 && retryCodes.Contains((int)response.StatusCode))
                {
                    return await retry(endpoint, body, retries, backoff);
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
