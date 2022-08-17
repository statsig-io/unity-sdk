using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;

namespace StatsigUnity
{
    public class EventLogger : MonoBehaviour, IDisposable
    {
        List<EventLog> _eventLogQueue;
        RequestDispatcher _dispatcher;
        HashSet<string> _errorsLogged;
        Dictionary<string, double> _loggedExposures = new Dictionary<string, double>();

        IEnumerator _flushCoroutine;

        void Awake()
        {
            _eventLogQueue = new List<EventLog>();
            _errorsLogged = new HashSet<string>();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                FlushEvents(false);
            }
        }

        void OnApplicationQuit()
        {
            StopCoroutine(_flushCoroutine);
            FlushEvents(true);
        }

        internal void Init(RequestDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _flushCoroutine = PeriodicFlush(Constants.CLIENT_MAX_LOGGER_WAIT_TIME_IN_SEC);
            StartCoroutine(_flushCoroutine);
        }

        internal void ResetExposureDedupeKeys()
        {
            _loggedExposures = new Dictionary<string, double>();
        }

        internal void LogGateExposure(
            StatsigUser user,
            string gateName,
            bool gateValue,
            string ruleID,
            List<Dictionary<string, string>> secondaryExposures)
        {
            var dedupeKey = $"gate:{gateName}:{ruleID}:{(gateValue ? "true" : "false")}";
            if (!ShouldLogExposure(dedupeKey))
            {
                return;
            }
            var exposure = new EventLog
            {
                User = user,
                EventName = Constants.GATE_EXPOSURE_EVENT,
                Metadata = new Dictionary<string, string>
                {
                    ["gate"] = gateName,
                    ["gateValue"] = gateValue ? "true" : "false",
                    ["ruleID"] = ruleID
                },
                SecondaryExposures = secondaryExposures,
            };
            Enqueue(exposure);
        }

        internal void LogConfigExposure(
            StatsigUser user,
            string configName,
            string ruleID,
            List<Dictionary<string, string>> secondaryExposures)
        {
            var dedupeKey = $"config:{configName}:{ruleID}";
            if (!ShouldLogExposure(dedupeKey))
            {
                return;
            }
            var exposure = new EventLog
            {
                User = user,
                EventName = Constants.CONFIG_EXPOSURE_EVENT,
                Metadata = new Dictionary<string, string>
                {
                    ["config"] = configName,
                    ["ruleID"] = ruleID,
                },
                SecondaryExposures = secondaryExposures,
            };
            Enqueue(exposure);
        }

        internal void LogLayerExposure(
            StatsigUser user,
            string layerName,
            string ruleId,
            string allocatedExperiment,
            string parameterName,
            bool isExplicit,
            List<Dictionary<string, string>> exposures)
        {
            var dedupeKey = $"config:{layerName}:{ruleId}:{allocatedExperiment}:{parameterName}:{isExplicit}";
            if (!ShouldLogExposure(dedupeKey))
            {
                return;
            }
            var exposure = new EventLog
            {
                User = user,
                EventName = Constants.LAYER_EXPOSURE_EVENT,
                Metadata = new Dictionary<string, string>
                {
                    ["config"] = layerName,
                    ["ruleID"] = ruleId,
                    ["allocatedExperiment"] = allocatedExperiment,
                    ["parameterName"] = parameterName,
                    ["isExplicitParameter"] = isExplicit ? "true" : "false",
                },
                SecondaryExposures = exposures,
            };
            Enqueue(exposure);
        }

        bool ShouldLogExposure(string dedupeKey)
        {
            var now = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            if (_loggedExposures.TryGetValue(dedupeKey, out double lastTime))
            {
                if (lastTime >= now - 600 * 1000)
                {
                    return false;
                }
            }
            _loggedExposures[dedupeKey] = now;
            return true;
        }

        internal void Enqueue(EventLog entry)
        {
            _eventLogQueue.Add(entry);
            if (_eventLogQueue.Count >= Constants.CLIENT_MAX_LOGGER_QUEUE_LENGTH)
            {
                FlushEvents(false);
            }
        }

        internal async Task Shutdown()
        {
            StopCoroutine(_flushCoroutine);
            await FlushEvents(true);
        }

        void IDisposable.Dispose()
        {
            StopCoroutine(_flushCoroutine);
            var task = FlushEvents(true);
            task.Wait();
        }

        IEnumerator PeriodicFlush(int delay)
        {
            while (true)
            {
                yield return new WaitForSeconds(delay);
                FlushEvents(false);
            }
        }

        async Task FlushEvents(bool shutdown)
        {
            if (_eventLogQueue.Count == 0)
            {
                return;
            }
            var snapshot = _eventLogQueue;
            _eventLogQueue = new List<EventLog>();
            _errorsLogged.Clear();

            var body = new Dictionary<string, object>
            {
                ["statsigMetadata"] = new Dictionary<string, string>
                {
                    ["sessionID"] = Guid.NewGuid().ToString(),
                    ["language"] = Application.systemLanguage.ToString(),
                    ["platform"] = Application.platform.ToString(),
                    ["appVersion"] = Application.version,
                    ["operatingSystem"] = SystemInfo.operatingSystem,
                    ["deviceModel"] = SystemInfo.deviceModel,
                    ["batteryLevel"] = SystemInfo.batteryLevel.ToString(),
                    ["sdkType"] = SDKDetails.SDKType,
                    ["sdkVersion"] = SDKDetails.SDKVersion,
                    ["unityVersion"] = Application.unityVersion
                },
                ["events"] = snapshot.Select(evt => evt.ToDictionary())
            };
            await _dispatcher.Fetch("log_event", body, shutdown ? 0 : 5, 1);
        }
    }
}
