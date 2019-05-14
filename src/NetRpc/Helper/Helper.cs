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

        public static async Task SendStreamAsync(Func<byte[], Task> publishBuffer, Func<Task> publishBufferEnd, Stream stream, CancellationToken token)
        {
            byte[] buffer = new byte[StreamBufferSize];
            int readCount = await stream.ReadAsync(buffer, 0, StreamBufferSize, token);
            while (readCount > 0)
            {
                if (readCount < StreamBufferSize)
                {
                    byte[] tempBs = new byte[readCount];
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

        public static bool HasStream(this object obj)
        {
            if (obj is null)
                return false;

            if (obj is Stream)
                return true;

            var type = obj.GetType();
            var propertyInfos = type.GetProperties();
            return propertyInfos.Any(i => i.PropertyType == typeof(Stream));
        }

        public static bool TryGetStream(this object obj, out Stream stream)
        {
            stream = default;

            if (obj == null)
                return false;

            if (obj is Stream objS)
            {
                stream = objS;
                return true;
            }

            var ps = obj.GetType().GetProperties();
            var found = ps.FirstOrDefault(i => i.PropertyType == typeof(Stream));
            if (found == null)
                return false;

            stream = (Stream)found.GetValue(obj);
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
            if (bytes == default)
                return default;

            using (var stream = new MemoryStream(bytes, 0, bytes.Length, false))
            {
                var formatter = new BinaryFormatter();
                var data = formatter.Deserialize(stream);
                stream.Flush();
                return (TObject)data;
            }
        }

        public static string ListToString<T>(this IEnumerable<T> list, string split)
        {
            StringBuilder sb = new StringBuilder();
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

        public static bool IsSerializable(this object obj)
        {
            return obj.GetType().IsSerializable;
        }

        public static string GetFullMethodName(this MethodInfo method)
        {
            return $"{method.DeclaringType}/{method.Name}";
        }

        public static MethodInfoDto GetMethodInfoDto(this MethodInfo method)
        {
            return new MethodInfoDto
            {
                GenericArguments = method.GetGenericArguments().ToList().ConvertAll(GetTypeName).ToArray(),
                FullName = method.GetFullMethodName()
            };
        }

        private static string GetTypeName(Type t)
        {
            if (IsSystemType(t))
                return t.FullName;
            return t.AssemblyQualifiedName;
        }

        private static bool IsSystemType(Type t)
        {
            var sn = t.Module.ScopeName;
            return sn == "System.Private.CoreLib.dll" || sn == "CommonLanguageRuntimeLibrary";
        }
    }
}