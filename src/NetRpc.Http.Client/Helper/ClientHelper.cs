using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;

namespace NetRpc.Http.Client
{
    public static class ClientHelper
    {
        private static readonly JsonSerializerSettings Js = new JsonSerializerSettings{ContractResolver = DtoContractResolver.Instance};

        public static object ToDtoObject(this string str, Type t)
        {
            if (string.IsNullOrEmpty(str))
                return default;

            return JsonConvert.DeserializeObject(str, t, Js);
        }

        public static string GetActionPath(Type contractType, MethodInfo methodInfo)
        {
            string contractPath;
            var contractRoute = contractType.GetCustomAttribute<HttpRouteAttribute>(true);
            if (contractRoute == null)
                contractPath = contractType.Name;
            else
                contractPath = contractRoute.Template;

            string path;
            var methodRoute = methodInfo.GetCustomAttribute<HttpRouteAttribute>(true);
            if (methodRoute == null)
            {
                if (contractRoute != null && contractRoute.TrimActionAsync)
                    path = $"{contractPath}/{methodInfo.Name.TrimEndString("Async")}";
                else
                    path = $"{contractPath}/{methodInfo.Name}";
            }
            else
            {
                path = methodRoute.Template;
            }

            return path;
        }

        public static bool HasStream(this Type t)
        {
            if (t == typeof(Stream))
                return true;

            var propertyInfos = t.GetProperties();
            return propertyInfos.Any(i => i.PropertyType == typeof(Stream));
        }

        public static PropertyInfo GetStreamPropertyInfo(this Type t)
        {
            if (t == typeof(Stream))
                return null;

            var propertyInfos = t.GetProperties();
            return propertyInfos.FirstOrDefault(i => i.PropertyType == typeof(Stream));
        }

        public static string ToDtoJson<T>(this T obj)
        {
            if (obj == null)
                return null;
            return JsonConvert.SerializeObject(obj, Js);
        }

        public static Type GetTypeFromReturnTypeDefinition(this Type returnTypeDefinition)
        {
            if (returnTypeDefinition.IsTaskT())
            {
                var at = returnTypeDefinition.GetGenericArguments()[0];
                return at;
            }

            return returnTypeDefinition;
        }
    }
}