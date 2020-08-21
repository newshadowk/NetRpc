using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
    public class RouteFilterAttribute : ActionFilterAttribute
    {
        private readonly Type _contactType;
        private readonly string? _methodName;
        private readonly string? _optionsName;

        public RouteFilterAttribute(Type contactType, string? methodName = null, string? optionsName = null)
        {
            _contactType = contactType;
            _optionsName = optionsName;
            _methodName = methodName;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var f = context.ServiceProvider.GetService(typeof(IClientProxyFactory));
            // ReSharper disable once PossibleNullReferenceException
            var mi = f.GetType().GetMethod(nameof(IClientProxyFactory.CreateProxy))!.MakeGenericMethod(_contactType);
            var clientProxy = (IClientProxy)mi.Invoke(f, new object?[] { _optionsName })!;
            var proxyMethodInfos = clientProxy.Proxy.GetType().GetMethods();
            var methodName = _methodName ?? context.InstanceMethod.MethodInfo.Name;

            var proxyM = proxyMethodInfos.FirstOrDefault(i => i.Name == methodName);
            if (proxyM == null)
                throw new InvalidOperationException($"Method {methodName} not found in ClientProxy");

            var tgtPi = proxyM.GetParameters();
            var newArgs = CreateArgs(context.Args, tgtPi); 
            var tgtRet = await proxyM.InvokeAsync(clientProxy.Proxy, newArgs);
            context.Result = ConvertParam(tgtRet, context.InstanceMethod.MethodInfo.ReturnType.GetTypeFromReturnTypeDefinition());
        }

        private static object?[] CreateArgs(object?[] srcObjs, ParameterInfo[] tgtPi)
        {
            if (srcObjs.Length != tgtPi.Length)
                throw new InvalidOperationException("RoutTo need match the args count.");

            var ret = new List<object?>();
            for (int i = 0; i < srcObjs.Length; i++) 
                ret.Add(ConvertParam(srcObjs[i], tgtPi[i].ParameterType));

            return ret.ToArray();
        }

        private static object? ConvertParam(object? srcObj, Type tgtObjType)
        {
            if (srcObj == null)
                return null;

            if (srcObj is Stream || 
                srcObj is Action ||
                srcObj.GetType().IsSystemTypeOrEnum())
                return srcObj;

            return srcObj.CreateAndCopy(tgtObjType);
        }
    }
}