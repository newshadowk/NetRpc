using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NetRpc
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
    public class RouteToAttribute : ActionFilterAttribute
    {
        private readonly string _methodName;
        private readonly IClientProxy _clientProxy;
        private readonly MethodInfo[] _proxyMethodInfos;

        public RouteToAttribute(Type contactType, string methodName = null, string optionsName = null)
        {
            //_clientProxy
            var f = (IClientProxyFactory)GlobalServiceProvider.ScopeProvider.GetService(typeof(IClientProxyFactory));
            // ReSharper disable once PossibleNullReferenceException
            var mi = f.GetType().GetMethod(nameof(IClientProxyFactory.CreateProxy)).MakeGenericMethod(contactType);
            _clientProxy = (IClientProxy)mi.Invoke(f, new object[] {optionsName});
            _methodName = methodName;
            _proxyMethodInfos = _clientProxy.Proxy.GetType().GetMethods();
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var methodName = _methodName ?? context.InstanceMethod.MethodInfo.Name;

            var proxyM = _proxyMethodInfos.FirstOrDefault(i => i.Name == methodName);
            if (proxyM == null)
                throw new InvalidOperationException($"Method {methodName} not found in ClientProxy");

            var tgtPi = proxyM.GetParameters();
            var newArgs = CreateArgs(context.Args, tgtPi); 
            var tgtRet = await proxyM.InvokeAsync(_clientProxy.Proxy, newArgs);
            context.Result = ConvertParam(tgtRet, context.InstanceMethod.MethodInfo.ReturnType.GetTypeFromReturnTypeDefinition());
        }

        private static object[] CreateArgs(object[] srcObjs, ParameterInfo[] tgtPi)
        {
            if (srcObjs.Length != tgtPi.Length)
                throw new InvalidOperationException("RoutTo need match the args count.");

            var ret = new List<object>();
            for (int i = 0; i < srcObjs.Length; i++) 
                ret.Add(ConvertParam(srcObjs[i], tgtPi[i].ParameterType));

            return ret.ToArray();
        }

        private static object ConvertParam(object srcObj, Type tgtObjType)
        {
            if (srcObj is Stream || 
                srcObj is Action ||
                Helper.IsSystemType(srcObj.GetType()))
                return srcObj;
            return CloneParam(srcObj, tgtObjType);
        }

        private static object CloneParam(object srcObj, Type tgtObjType)
        {
            var srcPs = srcObj.GetType().GetProperties();
            if (srcPs.Length == 0)
                return Helper.GetDefaultValue(tgtObjType);

            var tgtObj = Activator.CreateInstance(tgtObjType);
            var tgtPs = tgtObjType.GetProperties();
            foreach (var srcP in srcPs)
            {
                var foundTgtP = tgtPs.FirstOrDefault(i => srcP.Name == i.Name);
                if (foundTgtP != null) 
                    foundTgtP.SetValue(tgtObj, srcP.GetValue(srcObj, null), null);
            }

            return tgtObj;
        }
    }
}