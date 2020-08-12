using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenTracing;
using OpenTracing.Tag;

namespace NetRpc.OpenTracing
{
    public static class Helper
    {
        private static readonly JsonSerializerSettings Js = new JsonSerializerSettings {ContractResolver = DtoContractResolver.Instance};

        public static ISpan SetTagMethodObj(this ISpan span, IActionExecutingContext context, int maxLength, bool isForce = false)
        {
            span.SetTag(new StringTag("Name"), context.ContractMethod.MethodInfo.ToFullMethodName());

            if (!isForce && context.ContractMethod.IsTracerArgsIgnore)
            {
                span.SetTag(new StringTag("Args"), "[Ignore]");
                return span;
            }

            if (context.ContractMethod.MethodInfo.GetParameters().Length == 0)
                return span;

            var mergeArgTypeObj = context.ContractMethod.CreateMergeArgTypeObj(null, null, 0, context.PureArgs);
            span.SetTag(new StringTag("Args"), mergeArgTypeObj.ToDisplayJson(maxLength));
            return span;
        }

        public static ISpan SetTagReturn(this ISpan span, IActionExecutingContext context, int maxLength, bool isForce = false)
        {
            if (!isForce && context.ContractMethod.IsTraceReturnIgnore)
            {
                span.SetTag(new StringTag("Result"), "[Ignore]");
                return span;
            }

            if (context.ContractMethod.MethodInfo.ReturnType != typeof(void))
                span.SetTag(new StringTag("Result"), context.Result.ToDisplayJson(maxLength));

            return span;
        }

        public static string? ToDisplayJson<T>(this T obj, int maxLength)
        {
            if (obj == null)
                return null;

            if (obj is Stream)
                return "Stream";

            var s = JsonConvert.SerializeObject(obj, Js);
            if (maxLength > 0 && s.Length > maxLength)
                return s.Substring(0, maxLength) + "...";
            return s;
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

        public static void CopyBaggageItemsTo(this ISpanContext spanContext, ISpan span)
        {
            foreach (var pair in spanContext.GetBaggageItems())
                span.SetBaggageItem(pair.Key, pair.Value);
        }
    }
}