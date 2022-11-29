using System.Reflection;
using NetRpc.Contract;

namespace NetRpc;

public class ValueItemWrapper
{
    private readonly object?[] _pureArgs;
    private readonly object?[] _args;
    private readonly MethodInfo _contractMethodInfo;
    private readonly IServiceProvider _serviceProvider;
    private List<ValueItemGroup>? _viGroups;

    internal List<ValueItemGroup> ValueItemGroups
    {
        get
        {
            if (_viGroups == null)
            {
                _viGroups = GetValueItemGroups();
                return _viGroups;
            }

            return _viGroups;
        }
    }

    public ValueItemWrapper(object?[] pureArgs, object?[] args, MethodInfo contractMethodInfo, IServiceProvider serviceProvider)
    {
        _pureArgs = pureArgs;
        _args = args;
        _contractMethodInfo = contractMethodInfo;
        _serviceProvider = serviceProvider;
    }

    internal async Task ValueFilterInvokeAsync()
    {
        foreach (var i in ValueItemGroups)
            await GroupInvokeAsync(i);
    }

    private async Task GroupInvokeAsync(ValueItemGroup group)
    {
        foreach (var vi in group.Items)
        {
            await vi.ValueFilterAttribute.InvokeAsync(group.Context, _serviceProvider);
            SetValue(vi, group.Context.Value);
        }
    }

    private void SetValue(ValueItem item, object? value)
    {
        if (item.PureArgsIndex.HasValue)
        {
            _pureArgs[item.PureArgsIndex.Value] = value;
            _args[item.PureArgsIndex.Value] = value;
            return;
        }

        if (item.Parent != null)
            item.PropertyInfo!.SetValue(item.Parent, value);
    }

    private List<ValueItemGroup> GetValueItemGroups()
    {
        if (_pureArgs.Length == 0)
            return new List<ValueItemGroup>();

        List<ValueItemGroup> list = new();
        var pis = _contractMethodInfo.GetParameters();
        for (var i = 0; i < _pureArgs.Length; i++)
            list.AddRange(GetStart(pis[i], _pureArgs[i], i));

        return list;
    }

    private static List<ValueItemGroup> GetStart(ParameterInfo pi, object? obj, int index)
    {
        List<ValueItemGroup> groups = new();

        /*
        [V1]
        public class Obj5
        {
        }
        */
        List<ValueItem> list = new();
        foreach (var v in pi.ParameterType.GetCustomAttributes<ValueFilterAttribute>(true))
            list.Add(new ValueItem(v, index));
        groups.Add(new ValueItemGroup(obj, list));

        /*
        public class Obj5
        {
            public Task T1([V1]string s1)
        }
        */
        list = new List<ValueItem>();
        foreach (var v in pi.GetCustomAttributes<ValueFilterAttribute>(true))
            list.Add(new ValueItem(v, index));
        groups.Add(new ValueItemGroup(obj, list));


        if (pi.ParameterType.IsSystemTypeOrEnum())
            return groups;

        foreach (var i in pi.ParameterType.GetProperties())
            groups.AddRange(Get(i, obj == null ? null : i.GetValue(obj), obj));

        return groups;
    }

    private static List<ValueItemGroup> Get(PropertyInfo pi, object? piValue, object? parent)
    {
        List<ValueItemGroup> groups = new();

        List<ValueItem> list = new();
        foreach (var v in pi.GetCustomAttributes<ValueFilterAttribute>())
            list.Add(new ValueItem(v, parent, pi));
        groups.Add(new ValueItemGroup(piValue, list));

        if (pi.PropertyType.IsSystemTypeOrEnum())
            return groups;

        foreach (var i in pi.PropertyType.GetProperties())
            groups.AddRange(Get(i, piValue == null ? null : i.GetValue(piValue), piValue));

        return groups;
    }
}

internal class ValueItemGroup
{
    public List<ValueItem> Items { get; }

    public ValueContext Context { get; }

    public ValueItemGroup(object? value, List<ValueItem> items)
    {
        Items = items;
        Context = new ValueContext();
        Context.Value = value;
    }
}

internal class ValueItem
{
    public ValueFilterAttribute ValueFilterAttribute { get; }

    public ValueItem(ValueFilterAttribute valueFilterAttribute, int? pureArgsIndex)
    {
        ValueFilterAttribute = valueFilterAttribute;
        PureArgsIndex = pureArgsIndex;
    }

    public ValueItem(ValueFilterAttribute valueFilterAttribute, object? parent, PropertyInfo? propertyInfo)
    {
        ValueFilterAttribute = valueFilterAttribute;
        Parent = parent;
        PropertyInfo = propertyInfo;
    }

    public int? PureArgsIndex { get; }

    public object? Parent { get; }

    public PropertyInfo? PropertyInfo { get; }
}