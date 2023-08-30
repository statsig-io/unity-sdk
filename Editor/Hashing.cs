using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace StatsigUnity
{
    public static class Hashing
    {

        public static string DJB2(string value)
        {
            int hash = 0;
            for (int i = 0; i < value.Length; i++)
            {
                var character = value[i];
                hash = (hash << 5) - hash + character;
                hash = hash & hash; // Convert to 32bit integer
            }
            return hash.ToString();
        }
    }
}