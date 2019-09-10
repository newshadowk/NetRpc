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

        public static async Task SendStreamAsync(Func<byte[], Task> publishBuffer, Func<Task> publishBufferEnd, Stream stream, CancellationToken token)
        {
            var buffer = new byte[StreamBufferSize];

            var readCount = await stream.ReadAsync(buffer, 0, StreamBufferSize, token);
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
                IsPost = method.GetCustomAttribute<MQPostAttribute>(true) != null
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

        public static long? GetLength(this Stream stream)
        {
            if (stream == null)
                return null;

            try
            {
                return stream.Length;
            }
            catch
            {
                return null;
            }
        }

        private static string GetTypeName(Type t)
        {
            if (IsSystemType(t))
                return t.FullName;
            return t.AssemblyQualifiedName;
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

        public static Exception WarpException(Exception ex, RpcContext context = null)
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

        public static void ConvertStreamProgress(RpcContext context, int progressCount)
        {
            if (context.Callback == null)
                return;

            //http channel stream is ref read stream.
            if (!(context.Stream is BufferBlockStream))
                return;

            var rate = (double)progressCount / 100;
            var bbs = (BufferBlockStream)context.Stream;
            var totalCount = bbs.Length;
          
            bbs.Progress += (s, e) =>
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
    }
}