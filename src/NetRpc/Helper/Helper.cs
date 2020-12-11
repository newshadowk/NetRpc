using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetRpc.Contract;

namespace NetRpc
{
    public static class Helper
    {
        /// <summary>
        /// 82910 less than 85000, not in LOH.
        /// </summary>
        public const int StreamBufferSize = 81920;

        /// <summary>
        /// about 4 MB
        /// </summary>
        public const int StreamBufferCacheCount = 53;

        private static readonly string[] SizeSuffixes = {"bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"};

        public static string SizeSuffix(long value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(decimalPlaces));
            }

            if (value < 0)
            {
                return "-" + SizeSuffix(-value);
            }

            // ReSharper disable FormatStringProblem
            if (value == 0)
            {
                return string.Format("{0:n" + decimalPlaces + "} bytes", 0);
            }
            // ReSharper restore FormatStringProblem

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            var mag = (int) Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            var adjustedSize = (decimal) value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            // ReSharper disable FormatStringProblem
            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
            // ReSharper restore FormatStringProblem
        }

        [return: NotNullIfNotNull("list")]
        public static string? ListToStringForDisplay<T>(this IEnumerable<T>? list, string split, bool displayCount = true)
        {
            if (list == null)
                return null;

            StringBuilder sb = new();
            var toList = list as IList<T> ?? list.ToList();

            if (displayCount)
            {
                sb.Append("[Count:" + toList.Count + "]");
                sb.Append(split);
            }

            foreach (var s in toList)
            {
                sb.Append(s);
                sb.Append(split);
            }

            return sb.ToString().TrimEndString(split);
        }

        public static async Task SendStreamAsync(Func<ReadOnlyMemory<byte>, Task> publishBuffer, Func<Task> publishBufferEnd, Stream stream,
            CancellationToken token, Action? started = null)
        {
            using var bo = ArrayPool<byte>.Shared.RentOwner(StreamBufferSize);
            var readCount = await stream.GreedReadAsync(bo.Array, 0, StreamBufferSize, token);
            started?.Invoke();
            while (readCount > 0)
            {
                if (readCount < StreamBufferSize)
                {
                    await publishBuffer(bo.Array.AsMemory().Slice(0, readCount));
                    await publishBufferEnd();
                    return;
                }

                await publishBuffer(bo.Array.AsMemory().Slice(0, readCount));
                readCount = await stream.GreedReadAsync(bo.Array, 0, StreamBufferSize, token);
            }

            await publishBufferEnd();
        }

        [return: NotNullIfNotNull("list")]
        public static string? ListToString<T>(this IEnumerable<T>? list, string split)
        {
            if (list == null)
                return null;

            var sb = new StringBuilder();
            var toList = list as IList<T> ?? list.ToList();
            foreach (var s in toList)
            {
                sb.Append(s);
                sb.Append(split);
            }

            return sb.ToString().TrimEndString(split);
        }

        [return: NotNullIfNotNull("s")]
        public static string? TrimEndString(this string? s, string endStr)
        {
            if (s == null)
                return s;

            if (!s.EndsWith(endStr))
                return s;

            return s.TrimEndString(endStr.Length);
        }

        [return: NotNullIfNotNull("s")]
        public static string? TrimEndString(this string? s, int delLength)
        {
            if (s == null)
                return null;

            if (s.Length <= delLength)
                return "";

            return s.Substring(0, s.Length - delLength);
        }

        public static long GetLength(this Stream? stream)
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

        [return: NotNullIfNotNull("obj")]
        public static byte[]? ToBytes(this object? obj)
        {
            if (obj == null)
                return null;

            using var stream = new MemoryStream();
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            stream.Flush();
            return stream.ToArray();
        }

        [return: NotNullIfNotNull("bytes")]
        public static T ToObject<T>(this byte[]? bytes)
        {
            return (T) bytes.ToObject()!;
        }

        [return: NotNullIfNotNull("bytes")]
        public static object? ToObject(this byte[]? bytes)
        {
            if (bytes == null)
                return null;

            using var stream = new MemoryStream(bytes, 0, bytes.Length, false);
            var formatter = new BinaryFormatter();
            var data = formatter.Deserialize(stream);
            stream.Flush();
            return data;
        }

        [return: NotNullIfNotNull("stream")]
        public static byte[]? StreamToBytes(this Stream? stream)
        {
            if (stream == null)
                return null;

            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        public static Exception WarpException(Exception ex, ActionExecutingContext? context = null)
        {
            var bodyFe = ex as FaultException;

            //normally Exception
            if (bodyFe == null && !(ex is OperationCanceledException))
            {
                var gt = typeof(FaultException<>).MakeGenericType(ex.GetType());
                var fe = (FaultException) Activator.CreateInstance(gt, ex)!;
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

        public static Exception UnWarpException(Exception ex)
        {
            if (ex is FaultException fe)
            {
                if (fe.Detail != null)
                    return fe.Detail;
            }

            return ex;
        }

        public static void ConvertStreamProgress(ActionExecutingContext context, int progressCount)
        {
            if (context.Callback == null)
                return;

            //http channel stream is ref read stream.
            if (context.Stream == null || context.Stream.Length == 0)
                return;

            if (context.CallbackType != typeof(double))
                return;

            var rate = (double) progressCount / 100;
            var totalCount = context.Stream.Length;

            context.Stream.ProgressAsync += async (_, e) =>
            {
                var p = (double) e.Value / totalCount;
                if (p == 0)
                    return;

                var p2 = p * rate * 100 + 100;
                await context.Callback(Convert.ChangeType(p2, context.CallbackType));
            };

            var rawAction = context.Callback;
            context.Callback = o =>
            {
                if (o is double postP)
                {
                    double retP;
                    if (postP > 100)
                        retP = postP - 100;
                    else
                        retP = postP * (100 - progressCount) / 100 + progressCount;

                    return rawAction(Convert.ChangeType(retP, context.CallbackType));
                }

                throw new ArgumentException("Callback");
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

        public static bool IsPropertiesDefault<T>(this T? obj) where T : class
        {
            if (obj == null)
                return true;

            var properties = typeof(T).GetProperties();

            foreach (var p in properties)
            {
                var value = p.GetValue(obj, null);
                var defaultValue = p.PropertyType.GetDefaultValue();
                if (!Equals(value, defaultValue))
                    return false;
            }

            return true;
        }

        public static object? GetDefaultValue(this Type t)
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
            var tgtObj = Activator.CreateInstance(tgtObjType)!;
            tgtObj.CopyPropertiesFrom(srcObj);
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

        public static string ExceptionToString(this Exception? e)
        {
            if (e == null)
                return "";

            var msgContent = new StringBuilder($"\r\n\r\n[{e.GetType().Name}]\r\n");
            msgContent.Append(GetMsgContent(e));

            var lastE = new List<Exception?>();
            var currE = e.InnerException;
            lastE.Add(e);
            lastE.Add(currE);
            while (currE != null && !lastE.Contains(currE))
            {
                msgContent.Append($"\r\n[{currE.GetType().Name}]\r\n");
                msgContent.Append(GetMsgContent(e.InnerException!));
                currE = currE.InnerException;
                lastE.Add(currE);
            }

            return msgContent.ToString();
        }

        public static void AsyncWait(this Task task)
        {
            //pass the sync context.
            Task.Run(task.Wait).Wait();
        }

        public static object? GetPropertyValue(object? classInstance, PropertyInfo p)
        {
            if (classInstance == null)
                return p.PropertyType.GetDefaultValue();

            return classInstance.GetType().InvokeMember(p.Name, BindingFlags.GetProperty,
                null, classInstance, new object[] { });
        }

        public static ValueTask ComWriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer)
        {
            return stream.WriteAsync(buffer);
        }

        private static async Task<int> GreedReadAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var sumCount = 0;
            var nextCount = count;
            var currOffset = offset;
            while (true)
            {
                var currCount = await stream.ReadAsync(buffer, currOffset, nextCount, cancellationToken);
                if (currCount == 0)
                    break;

                sumCount += currCount;
                nextCount -= currCount;
                currOffset += currCount;

                if (nextCount == 0)
                    break;
            }

            return sumCount;
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
    }
}