using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NetRpc
{
    public sealed class Instance
    {
        public Type Type { get; }

        public object Target { get; }

        public List<InstanceMethod> Methods { get; } = new List<InstanceMethod>();

        public Contract Contract { get; }

        public Instance(Contract contract, object target)
        {
            Contract = contract;
            Target = target;
            Type = target.GetType();
            var list = InstanceCache.TypeActionFilters.GetOrAdd(Type, i => i.GetCustomAttributes<ActionFilterAttribute>(true).ToList());
            foreach (var m in Type.GetMethods())
            {
                InstanceCache.MethodActionFilters.GetOrAdd(m, i =>
                {
                    list.AddRange(i.GetCustomAttributes<ActionFilterAttribute>(true).ToList());
                    return list;
                });

                Methods.Add(new InstanceMethod(m, list));
            }
        }
    }

    public sealed class InstanceMethod
    {
        public MethodInfo MethodInfo { get; }

        public List<ActionFilterAttribute> ActionFilters { get; }

        public InstanceMethod(MethodInfo methodInfo, List<ActionFilterAttribute> actionFilters = null)
        {
            MethodInfo = methodInfo;

            if (actionFilters == null)
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var list = InstanceCache.TypeActionFilters.GetOrAdd(methodInfo.DeclaringType,
                    i => i.GetCustomAttributes<ActionFilterAttribute>(true).ToList());

                InstanceCache.MethodActionFilters.GetOrAdd(methodInfo,
                    i =>
                    {
                        list.AddRange(methodInfo.GetCustomAttributes<ActionFilterAttribute>(true).ToList());
                        return list;
                    });
            }
            else
                ActionFilters = actionFilters;
        }
    }

    internal static class InstanceCache
    {
        public static readonly ConcurrentDictionary<Type, List<ActionFilterAttribute>> TypeActionFilters =
            new ConcurrentDictionary<Type, List<ActionFilterAttribute>>();

        public static readonly ConcurrentDictionary<MethodInfo, List<ActionFilterAttribute>> MethodActionFilters =
            new ConcurrentDictionary<MethodInfo, List<ActionFilterAttribute>>();
    }
}