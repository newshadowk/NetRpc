using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using OpenTracing;
using OpenTracing.Tag;
using OpenTracing.Util;

namespace NetRpc.OpenTracing
{
    public static class Helper
    {
        private static readonly JsonSerializerSettings Js = new JsonSerializerSettings { ContractResolver = DtoContractResolver.Instance };

        public static ISpan SetTagMethodObj(this ISpan span, IActionExecutingContext context, int maxLength, bool isForce = false)
        {
            if (!isForce && context.ContractMethod.IsTraceArgsIgnore)
                return span;

            span.SetTag(new StringTag("Name"), context.ContractMethod.MethodInfo.ToFullMethodName());

            if (context.ContractMethod.MethodInfo.GetParameters().Length == 0)
                return span;

            var mergeArgTypeObj = context.ContractMethod.CreateMergeArgTypeObj(null, null, context.PureArgs);
            span.SetTag(new StringTag("Args"), mergeArgTypeObj.ToDisplayJson(maxLength));
            return span;
        }

        public static ISpan SetTagReturn(this ISpan span, IActionExecutingContext context, int maxLength, bool isForce = false)
        {
            if (!isForce && context.ContractMethod.IsTraceReturnIgnore)
                return span;

            if (context.ContractMethod.MethodInfo.ReturnType != typeof(void))
               span.SetTag(new StringTag("Result"), context.Result.ToDisplayJson(maxLength));

            return span;
        }

        public static string ToDisplayJson<T>(this T obj, int maxLength)
        {
            if (obj == null)
                return null;

            if (obj is Stream)
            {
                return "Stream";
            }

            string s = JsonConvert.SerializeObject(obj, Js);
            if (maxLength > 0 && s.Length > maxLength)
                return s.Substring(0, maxLength) + "...";
            return s;
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