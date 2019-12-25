using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public static class Helper
    {
        public const int StreamBufferSize = 81920;

        /// <summary>
        /// about 4 MB
        /// </summary>
        public const int StreamBufferCount = 53;

        private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string SizeSuffix(long value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException(nameof(decimalPlaces)); }
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

        public static string ListToStringForDisplay(Array list, string split)
        {
            var sb = new StringBuilder();

            sb.Append("[Count:" + list.Length + "]");
            sb.Append(split);

            foreach (var s in list)
            {
                sb.Append(s);
                sb.Append(split);
            }

            return sb.ToString().TrimEndString(split);
        }

        public static async Task SendStreamAsync(Func<byte[], Task> publishBuffer, Func<Task> publishBufferEnd, Stream stream, CancellationToken token,
            Action started = null)
        {
            var buffer = new byte[StreamBufferSize];

            var readCount = await stream.ReadAsync(buffer, 0, StreamBufferSize, token);
            started?.Invoke();
            while (readCount > 0)
            {
                if (readCount < StreamBufferSize)
                {
                    var tempBs = new byte[readCount];
                    Buffer.BlockCopy(buffer, 0, tempBs, 0, readCount);
                    await publishBuffer(tempBs);
                    await publishBufferEnd();
                    return;
                }

                await publishBuffer(buffer);
                readCount = await stream.ReadAsync(buffer, 0, StreamBufferSize, token);
            }

            await publishBufferEnd();
        }

        public static bool TryGetStream(this object obj, out Stream stream, out string streamName)
        {
            stream = default;
            streamName = default;

            if (obj == null)
                return false;

            if (obj is Stream objS)
            {
                stream = objS;
                return true;
            }

            //stream
            var ps = obj.GetType().GetProperties();
            var found = ps.FirstOrDefault(i => i.PropertyType == typeof(Stream));
            if (found == null)
                return false;
            stream = (Stream) found.GetValue(obj);

            //streamName
            found = ps.FirstOrDefault(i => i.Name == "StreamName");
            if (found != null)
                streamName = found.GetValue(obj) as string;

            return true;
        }

        public static object SetStream(this object obj, Stream stream)
        {
            if (obj == null)
                return stream;

            var ps = obj.GetType().GetProperties();
            var found = ps.FirstOrDefault(i => i.PropertyType == typeof(Stream));
            if (found == null)
                return obj;

            found.SetValue(obj, stream);
            return obj;
        }

        public static string ListToString<T>(this IEnumerable<T> list, string split)
        {
            var sb = new StringBuilder();
            var toList = list as IList<T> ?? list.ToList();
            foreach (var s in toList)
            {
                sb.Append(s);
                sb.Append(split);
            }

            return sb.ToString().TrimEndString(split);
        }

        public static string TrimEndString(this string s, string endStr)
        {
            if (!s.EndsWith(endStr))
                return s;

            return s.TrimEndString(endStr.Length);
        }

        public static string TrimEndString(this string s, int delLength)
        {
            if (s == null)
                return null;

            if (s.Length <= delLength)
                return "";

            return s.Substring(0, s.Length - delLength);
        }

        public static string ToFullMethodName(this MethodInfo method)
        {
            // ReSharper disable once PossibleNullReferenceException
            return $"{method.DeclaringType.Name}/{method.Name}";
        }

        public static ActionInfo ToActionInfo(this MethodInfo method)
        {
            return new ActionInfo
            {
                GenericArguments = method.GetGenericArguments().ToList().ConvertAll(GetTypeName).ToArray(),
                FullName = method.ToFullMethodName(),
            };
        }

        public static void AppendMethodInfo(this FaultException ex, ActionInfo action, object[] args)
        {
            if (!string.IsNullOrEmpty(ex.Action))
                ex.Action += " | ";

            ex.Action += $"{action}, {args.ListToString(", ")}";
            ex.Action = ex.Action.TrimEndString(", ");
        }

        public static bool IsActionT(this Type t)
        {
            if (t == null)
                return false;
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Action<>);
        }

        public static bool IsCancellationToken(this Type t)
        {
            return t == typeof(CancellationToken?) || t == typeof(CancellationToken);
        }

        public static bool IsTaskT(this Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>);
        }

        public static long GetLength(this Stream stream)
        {
            if (stream == null)
                return 0;

            try
            {
                return stream.Length;
            }
            catch
            {
                return 0;
            }
        }

        public static bool IsSystemType(Type t)
        {
            var sn = t.Module.ScopeName;
            return sn == "System.Private.CoreLib.dll" || sn == "CommonLanguageRuntimeLibrary";
        }

        public static byte[] ToBytes(this object obj)
        {
            if (obj == default)
                return default;

            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                stream.Flush();
                return stream.ToArray();
            }
        }

        public static T ToObject<T>(this byte[] bytes)
        {
            return (T) bytes.ToObject();
        }

        public static object ToObject(this byte[] bytes)
        {
            if (bytes == default)
                return default;

            using (var stream = new MemoryStream(bytes, 0, bytes.Length, false))
            {
                var formatter = new BinaryFormatter();
                var data = formatter.Deserialize(stream);
                stream.Flush();
                return data;
            }
        }

        public static byte[] StreamToBytes(this Stream stream)
        {
            if (stream == null)
                return null;

            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        public static Exception WarpException(Exception ex, ActionExecutingContext context = null)
        {
            var bodyFe = ex as FaultException;

            //normally Exception
            if (bodyFe == null && !(ex is OperationCanceledException))
            {
                var gt = typeof(FaultException<>).MakeGenericType(ex.GetType());
                var fe = (FaultException) Activator.CreateInstance(gt, ex);
                if (context != null)
                    fe.AppendMethodInfo(context.ActionInfo, context.Args);
                return fe;
            }

            //FaultException
            if (bodyFe != null)
            {
                if (context != null)
                    bodyFe.AppendMethodInfo(context.ActionInfo, context.Args);
                return bodyFe;
            }

            //OperationCanceledException
            return ex;
        }

        public static void ConvertStreamProgress(ActionExecutingContext context, int progressCount)
        {
            if (context.Callback == null)
                return;

            //http channel stream is ref read stream.
            if (context.Stream == null || context.Stream.Length == 0)
                return;

            var rate = (double)progressCount / 100;
            var totalCount = context.Stream.Length;

            context.Stream.Progress += (s, e) =>
            {
                var p = (double)e / totalCount;
                if (p == 0)
                    return;

                var p2 = p * rate * 100 + 100;
                context.Callback(Convert.ChangeType(p2, context.CallbackType));
            };

            var rawAction = context.Callback;
            context.Callback = o =>
            {
                var postP = (double)o;
                double retP;
                if (postP > 100)
                    retP = postP - 100;
                else
                    retP = postP * (100 - progressCount) / 100 + progressCount;

                rawAction(Convert.ChangeType(retP, context.CallbackType));
            };
        }

        public static void CopyPropertiesFrom<T>(this T toObj, T fromObj)
        {
            var properties = typeof(T).GetProperties();
            if (properties.Length == 0)
                return;

            foreach (var p in properties)
                p.SetValue(toObj, p.GetValue(fromObj, null), null);
        }

        public static bool IsPropertiesDefault<T>(this T obj)
        {
            if (obj == null)
                return true;

            var properties = typeof(T).GetProperties();

            foreach (var p in properties)
            {
                var value = p.GetValue(obj, null);
                var defaultValue = GetDefaultValue(p.PropertyType);

                if (!Equals(value, defaultValue))
                    return false;
            }

            return true;
        }

        public static object GetDefaultValue(Type t)
        {
            if (t.IsValueType)
                return Activator.CreateInstance(t);

            return null;
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

        public static object CreateAndCopy(this object srcObj, Type tgtObjType)
        {
            var tgtObj = Activator.CreateInstance(tgtObjType);
            srcObj.CopyPropertiesFrom(tgtObj);
            return tgtObj;
        }

        public static void CopyPropertiesFrom(this object toObj, object fromObj)
        {
            var srcPs = fromObj.GetType().GetProperties();
            if (srcPs.Length == 0)
                return;

            var tgtPs = toObj.GetType().GetProperties().ToList();
            tgtPs.RemoveAll(i => !i.CanWrite);
            foreach (var srcP in srcPs)
            {
                var foundTgtP = tgtPs.FirstOrDefault(i => srcP.Name == i.Name);
                if (foundTgtP != null)
                    foundTgtP.SetValue(toObj, srcP.GetValue(fromObj, null), null);
            }
        }

        public static string ExceptionToString(this Exception e)
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

        private static Stream BytesToStream(byte[] bytes)
        {
            if (bytes == null)
                return null;
            Stream stream = new MemoryStream(bytes);
            return stream;
        }

        private static string GetMsgContent(Exception ee)
        {
            string ret = ee.Message;
            if (ee.TargetSite != null)
                ret += $"\r\nTargetSite:{ee.TargetSite.DeclaringType?.Name}.{ee.TargetSite.Name}";
            if (!string.IsNullOrEmpty(ee.StackTrace))
                ret += "\r\n" + ee.StackTrace;
            ret += "\r\n";
            return ret;
        }

        private static string GetTypeName(Type t)
        {
            if (IsSystemType(t))
                return t.FullName;
            return t.AssemblyQualifiedName;
        }
    }
}