using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using StatsigUnity;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

public class ProxySmokeTests
{
    private StatsigClient _client;

    [Serializable]
    private class Report
    {
        public bool ok;
        public int requestCount;
        public string errors;
    }

    [UnityTest]
    public IEnumerator FullUrlsAndTokenRotationReachTheProxy()
    {
        var config = Resources.Load<TextAsset>("statsig-proxy-test-url");
        Assert.IsNotNull(config, "Add Resources/statsig-proxy-test-url.txt with the reachable server URL.");
        var baseUrl = config.text.Trim().TrimEnd('/');
        var query = "?run=" + Guid.NewGuid().ToString("N") + "&encoded=a%2Fb";
        _client = new StatsigClient("client-proxy-smoke", new StatsigOptions
        {
            InitializationUrl = baseUrl + "/lcf/statsig/client/v1/initialize" + query,
            EventLoggingUrl = baseUrl + "/lcf/statsig/client/v1/events" + query,
            RequestHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer smoke-A",
                ["Lcf-Client-App-Id"] = "unity-smoke"
            },
            InitializeTimeoutMs = 0,
            LoggingIntervalMs = 600000
        });

        yield return Complete(_client.Initialize(new StatsigUser { UserID = "smoke-a" }));
        _client.LogEvent("smoke-a");
        yield return Complete(_client.Flush());

        _client.LogEvent("queued-before-rotation");
        _client.SetRequestHeader("authorization", "Bearer smoke-B");
        yield return Complete(_client.UpdateUser(new StatsigUser { UserID = "smoke-b" }));
        yield return Complete(_client.Flush());

        _client.RemoveRequestHeader("AUTHORIZATION");
        yield return Complete(_client.UpdateUser(new StatsigUser { UserID = "smoke-c" }));
        _client.LogEvent("shutdown-event");
        yield return Complete(_client.Shutdown());
        _client = null;

        using (var request = UnityWebRequest.Get(baseUrl + "/report" + query))
        {
            request.timeout = 15;
            yield return request.SendWebRequest();
            Assert.AreEqual(200, request.responseCode, request.error);
            var report = JsonUtility.FromJson<Report>(request.downloadHandler.text);
            Assert.AreEqual(6, report.requestCount, report.errors);
            Assert.IsTrue(report.ok, report.errors);
        }
    }

    [UnityTearDown]
    public IEnumerator Shutdown()
    {
        if (_client != null)
        {
            yield return Complete(_client.Shutdown());
            _client = null;
        }
    }

    private static IEnumerator Complete(Task task)
    {
        var deadline = Time.realtimeSinceStartup + 30;
        while (!task.IsCompleted)
        {
            Assert.Less(Time.realtimeSinceStartup, deadline, "SDK request timed out.");
            yield return null;
        }
        task.GetAwaiter().GetResult();
    }
}
