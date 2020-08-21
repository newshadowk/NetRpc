using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NetRpc.Contract;

namespace NetRpc
{
    public static class TypeExtension
    {
        public static bool IsFuncT(this Type? t)
        {
            if (t == null)
                return false;
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Func<,>);
        }

        public static bool IsStream(this Type? t)
        {
            return t == typeof(Stream);
        }

        public static bool IsCancellationToken(this Type t)
        {
            return t == typeof(CancellationToken?) || t == typeof(CancellationToken);
        }

        public static bool IsTaskT(this Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>);
        }

        public static string ToFullMethodName(this MethodInfo method)
        {
            // ReSharper disable once PossibleNullReferenceException
            return $"{method.DeclaringType!.Name}/{method.Name}";
        }

        public static ActionInfo ToActionInfo(this MethodInfo method)
        {
            return new ActionInfo
            {
                GenericArguments = method.GetGenericArguments().ToList().ConvertAll(GetTypeName).ToArray(),
                FullName = method.ToFullMethodName()
            };
        }

        public static void AppendMethodInfo(this FaultException ex, ActionInfo action, object?[] args)
        {
            if (!string.IsNullOrEmpty(ex.Action))
                ex.Action += " | ";

            ex.Action += $"{action}, {args.ListToString(", ")}";
            ex.Action = ex.Action.TrimEndString(", ");
        }

        public static bool IsSystemType(this Type t)
        {
            var sn = t.Module.ScopeName;
            return sn == "System.Private.CoreLib.dll" || sn == "CommonLanguageRuntimeLibrary";
        }

        public static bool IsSystemTypeOrEnum(this Type t)
        {
            if (t.IsEnum)
                return true;
            var sn = t.Module.ScopeName;
            return sn == "System.Private.CoreLib.dll" || sn == "CommonLanguageRuntimeLibrary";
        }

        public static bool TryGetStream(this object? obj, out Stream? stream, out string? streamName)
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
            var found = ps.FirstOrDefault(i => i.PropertyType.IsStream());
            if (found == null)
                return false;
            stream = (Stream)found.GetValue(obj)!;

            //streamName
            found = ps.FirstOrDefault(i => i.Name.IsStreamName());
            if (found != null)
                streamName = found.GetValue(obj) as string;

            return true;
        }

        public static object SetStream(this object? obj, Stream stream)
        {
            if (obj == null)
                return stream;

            var ps = obj.GetType().GetProperties();
            var found = ps.FirstOrDefault(i => i.PropertyType.IsStream());
            if (found == null)
                return obj;

            found.SetValue(obj, stream);
            return obj;
        }

        public static bool IsStreamName(this string propName)
        {
            // ReSharper disable once StringLiteralTypo
            return propName.ToLower() == "streamname";
        }

        private static string GetTypeName(Type t)
        {
            if (t.IsSystemType())
                return t.FullName!;
            return t.AssemblyQualifiedName!;
        }
    }
}