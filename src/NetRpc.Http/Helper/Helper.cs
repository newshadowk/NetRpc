using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NetRpc.Http.Client;

namespace NetRpc.Http
{
    internal static class Helper
    {
        public static HttpDataObj ToHttpDataObj(string json, Type t)
        {
            var obj = ToObjectForHttp(json, t);
            if (obj == null)
                return null;

            var (connectionId, callId, streamLength) = GetAdditionData(obj);

            return new HttpDataObj
            {
                StreamLength = streamLength,
                Value = obj,
                CallId = callId,
                ConnectionId = connectionId,
                Type = t
            };
        }

        public static object ToObjectForHttp(string json, Type t)
        {
            object dataObj;
            try
            {
                dataObj = json.ToDtoObject(t);
            }
            catch (Exception e)
            {
                throw new HttpFailedException($"{e.Message}, str:\r\n{json}");
            }

            return dataObj;
        }

        public static string FormatPath(string path)
        {
            path = path.Replace('\\', '/');
            path = path.Replace("//", "/");
            path = path.TrimStart('/');
            return path;
        }

        public static object[] GetPureArgsFromDataObj(Type dataObjType, object dataObj)
        {
            var ret = new List<object>();
            if (dataObjType == null)
                return ret.ToArray();
            foreach (var p in dataObjType.GetProperties())
                ret.Add(GetPropertyValue(dataObj, p.Name));
            return ret.ToArray();
        }

        public static HttpDataObj GetHttpDataObjFromQuery(HttpRequest request, Type dataObjType)
        {
            var dataObj = GetDataObjFromQuery(request, dataObjType);

            return new HttpDataObj
            {
                StreamLength = 0,
                Value = dataObj,
                CallId = null,
                ConnectionId = null,
                Type = dataObj.GetType()
            };
        }

        public static bool IsEqualsOrSubclassOf(this Type t, Type c)
        {
            return t == c || t.IsSubclassOf(c);
        }

        public static List<string> GetCommentsXmlPaths()
        {
            var ret = new List<string>();
            var root = AppContext.BaseDirectory;
            foreach (var file in Directory.GetFiles(root))
            {
                if (string.Equals(Path.GetExtension(file), ".xml", StringComparison.OrdinalIgnoreCase))
                {
                    var dll = GetFullPathWithoutExtension(file) + ".dll";
                    var exe = GetFullPathWithoutExtension(file) + ".exe";
                    if (File.Exists(dll) || File.Exists(exe))
                        ret.Add(file);
                }
            }

            return ret;
        }

        public static string GetFullPathWithoutExtension(string s)
        {
            var dir = Path.GetDirectoryName(s);
            if (dir == null)
                dir = "";
            var name = Path.GetFileNameWithoutExtension(s);
            if (name == null)
                name = "";
            return Path.Combine(dir, name);
        }

        public static object GetPropertyValue(object classInstance, string propertyName)
        {
            return classInstance.GetType().InvokeMember(propertyName, BindingFlags.GetProperty,
                null, classInstance, new object[] { });
        }

        private static (string connectionId, string callId, long streamLength) GetAdditionData(object dataObj)
        {
            if (dataObj == null)
                return (null, null, 0);

            var connectionId = (string) GetValue(dataObj, ClientConstValue.ConnectionIdName);
            var callId = (string) GetValue(dataObj, ClientConstValue.CallIdName);
            var streamLengthObj = GetValue(dataObj, ClientConstValue.StreamLength);
            var streamLength = (long?) streamLengthObj ?? 0;
            return (connectionId, callId, streamLength);
        }

        private static object GetValue(object obj, string propertyName)
        {
            var pi = obj.GetType().GetProperty(propertyName);
            if (pi == null)
                return null;
            return pi.GetValue(obj);
        }

        private static object GetDataObjFromQuery(HttpRequest request, Type dataObjType)
        {
            if (dataObjType == null)
                return null;

            var dataObj = Activator.CreateInstance(dataObjType);
            var ps = dataObjType.GetProperties();
            var targetObj = dataObj;

            // dataObj is CustomObj? get inside properties.
            if (ps.Length == 1 && !ps[0].PropertyType.IsSystemType())
            {
                targetObj = Activator.CreateInstance(ps[0].PropertyType);
                ps[0].SetValue(dataObj, targetObj);
            }

            SetDataObj(request, targetObj);
       
            return dataObj;
        }

        private static void SetDataObj(HttpRequest request, object dataObj)
        {
            var ps = dataObj.GetType().GetProperties();
            foreach (var p in ps)
            {
                if (request.Query.TryGetValue(p.Name, out var values) ||
                    request.HasFormContentType && request.Form.TryGetValue(p.Name, out values))
                {
                    try
                    {
                        if (p.PropertyType == typeof(string))
                        {
                            p.SetValue(dataObj, values[0]);
                            continue;
                        }

                        // ReSharper disable once PossibleNullReferenceException
                        var parsedValue = p.PropertyType.GetMethod("Parse", new[] { typeof(string) }).Invoke(null, new object[] { values[0] });
                        p.SetValue(dataObj, parsedValue);
                    }
                    catch (Exception ex)
                    {
                        throw new HttpNotMatchedException($"http get, '{p.Name}' is not valid value, {ex.Message}");
                    }
                }
            }
        }
    }
}