using System;
using Newtonsoft.Json;

namespace NetRpc.Http
{
    internal static class Helper
    {
        public static object ToObject(this string str, Type t)
        {
            if (string.IsNullOrEmpty(str))
                return default;

            return JsonConvert.DeserializeObject(str, t);
        }

        public static object ToObject(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return default;

            return JsonConvert.DeserializeObject(str);
        }

        public static string ToJson<T>(this T obj)
        {
            if (obj == null)
                return null;
            return JsonConvert.SerializeObject(obj);
        }

        public static string FormatPath(string path)
        {
            path = path.Replace('\\', '/');
            path = path.Replace("//", "/");
            path = path.TrimStart('/');
            return path;
        }

        public static string TrimToEndStr(this string srcStr, string endStr)
        {
            if (srcStr == null)
                return null;

            var i = srcStr.LastIndexOf(endStr, StringComparison.Ordinal);
            if (i == -1)
                return srcStr;

            return srcStr.Substring(0, i);
        }
    }
}