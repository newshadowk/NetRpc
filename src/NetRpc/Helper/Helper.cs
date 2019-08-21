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

        public static TObject ToObject<TObject>(this byte[] bytes)
        {
            return (TObject) bytes.ToObject();
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

        public static string GetFullMethodName(this MethodInfo method)
        {
            // ReSharper disable once PossibleNullReferenceException
            return $"{method.DeclaringType.Name}/{method.Name}";
        }

        public static ActionInfo GetMethodInfoDto(this MethodInfo method)
        {
            return new ActionInfo
            {
                GenericArguments = method.GetGenericArguments().ToList().ConvertAll(GetTypeName).ToArray(),
                FullName = method.GetFullMethodName(),
                IsPost = method.GetCustomAttributes<NetRpcPostAttribute>(true).Any()
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

        public static string ExceptionToString(Exception e)
        {
            if (e == null)
                return "";

            var msgContent = new StringBuilder("\r\n");
            msgContent.Append(GetMsgContent(e));

            var lastE = new List<Exception>();
            var currE = e.InnerException;
            lastE.Add(e);
            lastE.Add(currE);
            while (currE != null && !lastE.Contains(currE))
            {
                msgContent.Append("\r\n[InnerException]\r\n");
                msgContent.Append(GetMsgContent(e.InnerException));
                currE = currE.InnerException;
                lastE.Add(currE);
            }

            return msgContent.ToString();
        }

        private static string GetMsgContent(Exception ee)
        {
            var ret = ee.Message;
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

        public static bool IsSystemType(Type t)
        {
            var sn = t.Module.ScopeName;
            return sn == "System.Private.CoreLib.dll" || sn == "CommonLanguageRuntimeLibrary";
        }
    }
}