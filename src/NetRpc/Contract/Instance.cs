using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NetRpc;

public sealed class Instance
{
    public Type Type { get; }

    public object Target { get; }

    public List<InstanceMethod> Methods { get; } = new();

    public ContractInfo Contract { get; }

    public Instance(ContractInfo contract, object target)
    {
        Contract = contract;
        Target = target;
        Type = target.GetType();
        foreach (var m in Type.GetMethods())
            Methods.Add(new InstanceMethod(m, target));
    }
}

public sealed class InstanceMethod
{
    public MethodInfo MethodInfo { get; }

    public List<ActionFilterAttribute> ActionFilters { get; } = new();

    public InstanceMethod(MethodInfo methodInfo, object? target = null)
    {
        MethodInfo = methodInfo;

        if (target is ActionFilterAttribute targetF)
            ActionFilters.Add(targetF);

        // ReSharper disable once AssignNullToNotNullAttribute
        var cloneList = InstanceCache.InstanceTypeActionFilters.GetOrAdd(methodInfo.DeclaringType!,
            i => i.GetCustomAttributes<ActionFilterAttribute>(true).ToList()).ToList();

        ActionFilters.AddRange(InstanceCache.MethodActionFilters.GetOrAdd(methodInfo,
            _ =>
            {
                cloneList.AddRange(methodInfo.GetCustomAttributes<ActionFilterAttribute>(true).ToList());
                return cloneList;
            }));
    }
}

internal static class InstanceCache
{
    public static readonly ConcurrentDictionary<Type, List<ActionFilterAttribute>> InstanceTypeActionFilters =
        new();

    public static readonly ConcurrentDictionary<MethodInfo, List<ActionFilterAttribute>> MethodActionFilters =
        new();
}