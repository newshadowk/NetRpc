using System;
using System.Linq;
using Newtonsoft.Json;

namespace NetRpc.Http.Client
{
    public static class ClientHelper
    {
        private static readonly JsonSerializerSettings Js = new JsonSerializerSettings{ContractResolver = DtoContractResolver.Instance};

        public static object? ToDtoObject(this string str, Type t)
        {
            if (string.IsNullOrEmpty(str))
                return default;

            return JsonConvert.DeserializeObject(str, t, Js);
        }

        public static bool HasStream(this Type? t)
        {
            if (t == null)
                return false;

            if (t.IsStream())
                return true;

            var propertyInfos = t.GetProperties();
            return propertyInfos.Any(i => i.PropertyType.IsStream());
        }

        public static string? ToDtoJson<T>(this T obj)
        {
            if (obj == null)
                return null;
            return JsonConvert.SerializeObject(obj, Js);
        }
    }
}