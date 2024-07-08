using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace StatsigUnity
{
    public static class Hashing
    {

        public static string DJB2(string value)
        {
            long hash = 0;
            for (int i = 0; i < value.Length; i++)
            {
                var character = value[i];
                hash = (hash << 5) - hash + character;
                hash = hash & hash;
            }
            return (hash & ((1L << 32) - 1)).ToString();
        }
    }
}