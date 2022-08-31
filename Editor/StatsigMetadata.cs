using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace StatsigUnity
{
    public abstract class StatsigMetadata
    {
        public static Dictionary<string, string> AsDictionary(string stableId)
        {
            return new Dictionary<string, string>
            {
                ["sessionID"] = Guid.NewGuid().ToString(),
                ["stableID"] = stableId,
                ["language"] = Application.systemLanguage.ToString(),
                ["platform"] = Application.platform.ToString(),
                ["appVersion"] = Application.version,
                ["operatingSystem"] = SystemInfo.operatingSystem,
                ["deviceModel"] = SystemInfo.deviceModel,
                ["batteryLevel"] = SystemInfo.batteryLevel.ToString(CultureInfo.CurrentCulture),
                ["sdkType"] = SDKDetails.SDKType,
                ["sdkVersion"] = SDKDetails.SDKVersion,
                ["unityVersion"] = Application.unityVersion
            };
        }
    }
}