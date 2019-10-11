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

            var objs = new List<object>();
            var psValueList = psValue.ToList();

            //Sort params
            for (var i = 0; i < ps.Length; i++)
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
                var ret = (object) invokeRet.GetAwaiter().GetResult();
                return ret;
            }

            return invokeRet;
        }

        /// <exception cref="TypeLoadException"></exception>
        public static ServiceContext Convert(ServiceCallParam scp, List<Instance> instances, IServiceProvider serviceProvider)
        {
            var (instanceMethodInfo, methodObj, instance) = GetMethodInfo(scp.Action, instances);
            var ps = methodObj.MethodInfo.GetParameters();
            var args = GetArgs(ps, scp.PureArgs, scp.Callback, scp.Token, scp.Stream);
            return new ServiceContext(
                serviceProvider,
                scp.Header,
                instance.Value,
                instanceMethodInfo, 
                methodObj.MethodInfo, 
                args,
                scp.PureArgs,
                scp.Action, 
                scp.Stream,
                instance.Contract,
                scp.Callback, 
                scp.Token);
        }

        /// <exception cref="TypeLoadException"></exception>
        public static (MethodInfo instanceMethodInfo, MethodObj methodObj, Instance instance) GetMethodInfo(ActionInfo action, List<Instance> instances)
        {
            foreach (var o in instances)
            {
                var found = GetMethodInfo(action, o.Contract);
                if (found != default)
                    return (found.instanceMethodInfo, found.methodObj, o);
            }

            throw new MethodNotFoundException($"{action.FullName} not found in instances");
        }

        public static (MethodInfo instanceMethodInfo, MethodObj methodObj) GetMethodInfo(ActionInfo action, List<Contract> contracts)
        {
            foreach (var contract in contracts)
            {
                var found = GetMethodInfo(action, contract);
                if (found != default)
                    return found;
            }

            throw new MethodNotFoundException($"{action.FullName} not found in instanceTypes");
        }

        private static (MethodInfo instanceMethodInfo, MethodObj methodObj) GetMethodInfo(ActionInfo action, Contract contract)
        {
            var methodObj = contract.MethodObjs.FirstOrDefault(i => i.MethodInfo.ToFullMethodName() == action.FullName);
            if (methodObj != null)
            {
                var instanceMethodInfo = (contract.InstanceType ?? contract.ContractType).GetMethod(methodObj.MethodInfo.Name);
                if (action.GenericArguments.Length > 0)
                {
                    var ts = action.GenericArguments.ToList().ConvertAll(Type.GetType).ToArray();
                    // ReSharper disable once PossibleNullReferenceException
                    instanceMethodInfo = instanceMethodInfo.MakeGenericMethod(ts);
                }

                return (instanceMethodInfo, methodObj);
            }

            return default;
        }

        public static IEnumerable<MethodInfo> GetInterfaceMethods(this Type type)
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static |
                                                   BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                yield return method;
            }

            foreach (var i in type.GetInterfaces())
            {
                foreach (var method in GetInterfaceMethods(i))
                {
                    yield return method;
                }
            }
        }
    }
}