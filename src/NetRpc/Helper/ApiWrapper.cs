using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal static class ApiWrapper
    {
        private static object[] GetArgs(ParameterInfo[] ps, object[] psValue, Action<object> callback, CancellationToken token, BufferBlockStream stream)
        {
            var psList = ps.ToList();

            var dic = new Dictionary<int, object>();

            //Action<>
            var found = psList.FirstOrDefault(i => i.ParameterType.IsGenericType && i.ParameterType.GetGenericTypeDefinition() == typeof(Action<>));
            if (found != null)
                dic.Add(psList.IndexOf(found), ActionHelper.ConvertAction(callback, found.ParameterType.GetGenericArguments()[0]));

            //CancellationToken
            found = psList.FirstOrDefault(i => i.ParameterType == typeof(CancellationToken?) || i.ParameterType == typeof(CancellationToken));
            if (found != null)
                dic.Add(psList.IndexOf(found), token);

            //Stream
            found = psList.FirstOrDefault(i => i.ParameterType == typeof(Stream));
            if (found != null)
                dic.Add(psList.IndexOf(found), stream);

            List<object> objs = new List<object>();
            var psValueList = psValue.ToList();

            //Sort params
            for (int i = 0; i < ps.Length; i++)
            {
                if (dic.Keys.Any(j => j == i))
                {
                    objs.Add(dic[i]);
                }
                else
                {
                    objs.Add(psValueList[0]);
                    psValueList.RemoveAt(0);
                }
            }

            return objs.ToArray();
        }

        public static async Task<object> GetTaskResult(dynamic invokeRet, bool isGenericType)
        {
            if (invokeRet is Task)
            {
                await invokeRet;

                //Task
                if (!isGenericType)
                    return null;

                //Task<>
                var ret = (object)invokeRet.GetAwaiter().GetResult();
                return ret;
            }

            return invokeRet;
        }

        /// <exception cref="TypeLoadException"></exception>
        public static ApiContext Convert(ServiceCallParam scp, object[] instances)
        {
            (MethodInfo method, object instance) = GetMethodInfo(scp.Action, instances);
            var ps = method.GetParameters();
            var args = GetArgs(ps, scp.Args, scp.Callback, scp.Token, scp.Stream);
            return new ApiContext(scp.Header, instance, method, args);
        }

        /// <exception cref="TypeLoadException"></exception>
        private static (MethodInfo method, object instance) GetMethodInfo(ActionInfo action, object[] instances)
        {
            foreach (var o in instances)
            {
                var found = GetMethodInfo(action, o);
                if (found != null)
                    return (found, o);
            }
            throw new TypeLoadException($"{action.FullName} not found in instances");
        }

        private static MethodInfo GetMethodInfo(ActionInfo action, object instance)
        {
            var instanceType = instance.GetType();
            foreach (var item in instanceType.GetInterfaces())
            {
                var found = item.GetMethods().FirstOrDefault(i => i.GetFullMethodName() == action.FullName);
                if (found != null)
                {
                    var retM = instanceType.GetMethod(found.Name);
                    if (action.GenericArguments.Length > 0)
                    {
                        var ts = action.GenericArguments.ToList().ConvertAll(Type.GetType).ToArray();
                        // ReSharper disable once PossibleNullReferenceException
                        retM = retM.MakeGenericMethod(ts);
                    }
                    return retM;
                }
            }

            return null;
        }
    }
}