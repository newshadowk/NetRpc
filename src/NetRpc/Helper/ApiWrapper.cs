using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    public static class ApiWrapper
    {
        private static object[] GetArgs(ParameterInfo[] ps, object[] psValue, Action<object> callback, CancellationToken token, Stream stream)
        {
            var psList = ps.ToList();

            var dic = new Dictionary<int, object>();

            //Action<>
            var found = psList.FirstOrDefault(i => i.ParameterType.IsActionT());
            if (found != null)
                dic.Add(psList.IndexOf(found), ActionHelper.ConvertAction(callback, found.ParameterType.GetGenericArguments()[0]));

            //CancellationToken
            found = psList.FirstOrDefault(i => i.ParameterType.IsCancellationToken());
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
        public static RpcContext Convert(ServiceCallParam scp, object[] instances, IServiceProvider serviceProvider)
        {
            (MethodInfo instanceMethodInfo, MethodInfo contractMethodInfo, object instance) = GetMethodInfo(scp.Action, instances);
            var ps = contractMethodInfo.GetParameters();
            var args = GetArgs(ps, scp.Args, scp.Callback, scp.Token, scp.Stream);
            return new RpcContext
            {
                Callback = scp.Callback,
                ActionInfo = scp.Action,
                Args = args,
                ContractMethodInfo = contractMethodInfo,
                InstanceMethodInfo = instanceMethodInfo,
                ServiceProvider = serviceProvider,
                Header = scp.Header,
                Stream = scp.Stream,
                Target = instance,
                Token = scp.Token
            };
        }

        /// <exception cref="TypeLoadException"></exception>
        public static (MethodInfo instanceMethodInfo, MethodInfo contractMethodInfo, object instance) GetMethodInfo(ActionInfo action, object[] instances)
        {
            foreach (var o in instances)
            {
                var found = GetMethodInfo(action, o.GetType());
                if (found != default)
                    return (found.contractMethodInfo, found.contractMethodInfo, o);
            }
            throw new MethodNotFoundException($"{action.FullName} not found in instances");
        }

        public static (MethodInfo instanceMethodInfo, MethodInfo contractMethodInfo) GetMethodInfo(ActionInfo action, Type[] instanceTypes)
        {
            foreach (var t in instanceTypes)
            {
                var found = GetMethodInfo(action, t);
                if (found != default)
                    return found;
            }
            throw new MethodNotFoundException($"{action.FullName} not found in instanceTypes");
        }

        private static (MethodInfo instanceMethodInfo, MethodInfo contractMethodInfo) GetMethodInfo(ActionInfo action, Type instanceType)
        {
            foreach (var item in instanceType.GetInterfaces())
            {
                var contractMethodInfo = item.GetMethods().FirstOrDefault(i => i.GetFullMethodName() == action.FullName);
                if (contractMethodInfo != null)
                {
                    var instanceMethodInfo = instanceType.GetMethod(contractMethodInfo.Name);
                    if (action.GenericArguments.Length > 0)
                    {
                        var ts = action.GenericArguments.ToList().ConvertAll(Type.GetType).ToArray();
                        // ReSharper disable once PossibleNullReferenceException
                        instanceMethodInfo = instanceMethodInfo.MakeGenericMethod(ts);
                    }
                    return (instanceMethodInfo, contractMethodInfo);
                }
            }

            return default;
        }
    }
}