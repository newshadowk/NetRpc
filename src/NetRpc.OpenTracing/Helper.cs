using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using OpenTracing;
using OpenTracing.Tag;

namespace NetRpc.OpenTracing
{
    public static class Helper
    {
        private static readonly JsonSerializerSettings Js = new JsonSerializerSettings { ContractResolver = DtoContractResolver.Instance };

        public static ISpan SetTagMethodObj(this ISpan span, MethodObj methodObj, object[] args)
        {
            var mergeArgTypeObj = methodObj.CreateMergeArgTypeObj(null, null, args);
            span.SetTag(new StringTag("Name"), methodObj.MethodInfo.ToFullMethodName());
            span.SetTag(new StringTag("Params"), mergeArgTypeObj.ToDtoJson());
            return span;
        }

        public static string ToDtoJson<T>(this T obj)
        {
            if (obj == null)
                return null;
            return JsonConvert.SerializeObject(obj, Js);
        }

        public static object ToDtoObject(this string str, Type t)
        {
            if (string.IsNullOrEmpty(str))
                return default;

            return JsonConvert.DeserializeObject(str, t, Js);
        }

        public static bool HasStream(this Type t)
        {
            if (t == typeof(Stream))
                return true;

            var propertyInfos = t.GetProperties();
            return propertyInfos.Any(i => i.PropertyType == typeof(Stream));
        }

        public static string GetException(Exception e)
        {
            if (e == null)
                return "";

            var msgContent = new StringBuilder($"\r\n\r\n[{e.GetType().Name}]\r\n");
            msgContent.Append(GetMsgContent(e));

            List<Exception> lastE = new List<Exception>();
            Exception currE = e.InnerException;
            lastE.Add(e);
            lastE.Add(currE);
            while (currE != null && !lastE.Contains(currE))
            {
                msgContent.Append($"\r\n[{currE.GetType().Name}]\r\n");
                msgContent.Append(GetMsgContent(e.InnerException));
                currE = currE.InnerException;
                lastE.Add(currE);
            }

            return msgContent.ToString();
        }

        private static string GetMsgContent(Exception ee)
        {
            string ret = ee.Message;
            if (!string.IsNullOrEmpty(ee.StackTrace))
                ret += "\r\n" + ee.StackTrace;
            ret += "\r\n";
            return ret;
        }
    }
}