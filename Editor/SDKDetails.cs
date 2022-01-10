using System;
using System.Collections.Generic;

namespace StatsigUnity
{
    public static class SDKDetails
    {
        internal static string SDKType = "unity";
        internal static string SDKVersion = "0.1.0";
        internal static IReadOnlyDictionary<string, string> StatsigMetadata
        {
            get
            {
                return new Dictionary<string, string>
                {
                    ["sdkType"] = SDKDetails.SDKType,
                    ["sdkVersion"] = SDKDetails.SDKVersion,
                };
            }
        }
    }
}
