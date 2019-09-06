using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NetRpc.Http.Client
{
    public static class ClientHelper
    {
        public static object ToObject(this string str, Type t)
        {
            if (string.IsNullOrEmpty(str))
                return default;

            return JsonConvert.DeserializeObject(str, t);
        }

        public static async Task RetryAsync(Task task, CancellationToken token, Action<string, Exception> log, int retryCount = 2, int retryIntervalMs = 1000)
        {
            var currentRetry = 0;
            while (true)
            {
                try
                {
                    await task;
                    return;
                }
                catch (TaskCanceledException)
                {
                    log("RetryTask, TaskCanceledException catched, return.", null);
                    throw;
                }
                catch (Exception ex)
                {
                    log("RetryTask,", ex);
                    if (currentRetry >= retryCount)
                        throw;
                }

                if (token.IsCancellationRequested)
                {
                    log("RetryTask, token.IsCancellationRequested, return.", null);
                    return;
                }

                currentRetry++;

                log($"{currentRetry}/{retryCount} times, wait {retryIntervalMs / 1000} seconds to retry... ", null);
                // ReSharper disable once MethodSupportsCancellation
                Task.Delay(retryIntervalMs, token).Wait();
                log($"{currentRetry}/{retryCount} times, retry", null);
            }
        }

        public static Type GetArgType(MethodInfo m, bool supportCallbackAndCancel, out string streamName, out TypeName action, out TypeName cancelToken)
        {
            streamName = null;
            action = null;
            cancelToken = null;

            var typeName = $"{m.Name}Param";
            var t = ClassHelper.BuildType(typeName);
            var cis = new List<ClassHelper.CustomsPropertyInfo>();

            bool addedCallId = false;
            foreach (var p in m.GetParameters())
            {
                if (p.ParameterType == typeof(Stream))
                {
                    streamName = p.Name;
                    continue;
                }

                //callback
                if (p.ParameterType.IsActionT())
                {
                    if (!supportCallbackAndCancel)
                        continue;

                    action = new TypeName
                    {
                        Type = p.ParameterType,
                        Name = p.Name
                    };

                    addedCallId = true;

                    continue;
                }

                //cancel
                if (p.ParameterType == typeof(CancellationToken?) || p.ParameterType == typeof(CancellationToken))
                {
                    if (!supportCallbackAndCancel)
                        continue;

                    cancelToken = new TypeName
                    {
                        Type = p.ParameterType,
                        Name = p.Name
                    };

                    addedCallId = true;

                    continue;
                }

                cis.Add(new ClassHelper.CustomsPropertyInfo(p.ParameterType, p.Name));
            }

            //connectionId callId
            if (addedCallId)
            {
                cis.Add(new ClassHelper.CustomsPropertyInfo(typeof(string), ClientConstValue.ConnectionIdName));
                cis.Add(new ClassHelper.CustomsPropertyInfo(typeof(string), ClientConstValue.CallIdName));
            }

            t = ClassHelper.AddProperty(t, cis);
            if (cis.Count == 0)
                return null;
            return t;
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

        public static string ToJson<T>(this T obj)
        {
            if (obj == null)
                return null;
            return JsonConvert.SerializeObject(obj);
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