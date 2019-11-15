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

        private static readonly string[] SizeSuffixes =
            { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }


        public static ISpan SetTagMethodObj(this ISpan span, IActionExecutingContext context, int maxLength, bool isForce = false)
        {
            if (!isForce && context.ContractMethod.IsTracerArgsIgnore)
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

        public static void CopyBaggageItemsTo(this ISpanContext spanContext, ISpan span)
        {
            foreach (var pair in spanContext.GetBaggageItems()) 
                span.SetBaggageItem(pair.Key, pair.Value);
        }

        public static bool GetIsLogDetails(OpenTracingOptions options)
        {
            if (GlobalTracer.Instance.ActiveSpan == null)
                return true;

            var isLogDetailsStr = GlobalTracer.Instance.ActiveSpan.GetBaggageItem(ConstValue.IsLogDetails);
            if (isLogDetailsStr == null)
                return options.IsLogDetails;
            return bool.Parse(isLogDetailsStr);
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