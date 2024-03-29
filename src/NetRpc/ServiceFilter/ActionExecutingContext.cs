﻿using System.Reflection;
using NetRpc.Contract;

namespace NetRpc;

public class ActionExecutingContext : IActionExecutingContext
{
    private object? _result;

    public event EventHandler? SendResultStreamStarted;

    public event EventHandler? SendResultStreamEndOrFault;

    public ChannelType ChannelType { get; }

    public DateTimeOffset StartTime { get; }

    public IServiceProvider ServiceProvider { get; }
    public Dictionary<string, object?> Header { get; set; }

    public InstanceMethod InstanceMethod { get; }

    public ContractMethod ContractMethod { get; }

    public Instance Instance { get; }

    public ContractInfo Contract { get; }

    public Type? CallbackType { get; }

    public Func<object?, Task>? Callback
    {
        get
        {
            var found = Args.FirstOrDefault(i =>
            {
                if (i == null)
                    return false;

                return i.GetType().IsFuncT();
            });
            return FuncHelper.ConvertFunc(found);
        }
        set
        {
            for (var i = 0; i < Args.Length; i++)
            {
                if (Args[i] == null)
                    continue;

                var t = Args[i]?.GetType();
                if (t.IsFuncT())
                {
                    Args[i] = FuncHelper.ConvertFunc(value, CallbackType);
                    return;
                }
            }
        }
    }

    public CancellationToken CancellationToken { get; }

    public ProxyStream? Stream { get; }

    public object?[] Args { get; }

    /// <summary>
    /// Args of invoked action without stream and action.
    /// </summary>
    public object?[] PureArgs { get; }

    public ActionInfo ActionInfo { get; }

    public ValueItemWrapper ValueItemWrapper { get; }

    /// <summary>
    /// A central location for sharing state between components during the invoking process.
    /// </summary>
    public Dictionary<object, object?> Properties { get; set; }

    public ActionExecutingContext(IServiceProvider serviceProvider,
        Dictionary<string, object?> header,
        Instance instance,
        MethodInfo instanceMethodInfo,
        ContractMethod contractMethod,
        object?[] args,
        object?[] pureArgs,
        ActionInfo actionInfo,
        ProxyStream? stream,
        ContractInfo contract,
        ChannelType channelType,
        Func<object?, Task>? callback,
        CancellationToken token)
    {
        Properties = new Dictionary<object, object?>();
        StartTime = DateTimeOffset.Now;
        ServiceProvider = serviceProvider;
        ChannelType = channelType;
        Header = header;
        InstanceMethod = instance.Methods.Find(i => i.MethodInfo == instanceMethodInfo)!;
        ContractMethod = contractMethod;
        Instance = instance;
        Args = args;
        PureArgs = pureArgs;
        CallbackType = GetFuncType(args);
        ActionInfo = actionInfo;
        Callback = callback;
        Stream = stream;
        Contract = contract;
        CancellationToken = token;
        ValueItemWrapper = new ValueItemWrapper(pureArgs, args, contractMethod.ParameterInfos, serviceProvider);
        ResetProps();
    }

    /// <summary>
    /// Gets or sets value inside an action filter will short-circuit the action and any remaining action filters.
    /// </summary>
    public object? Result
    {
        get => _result;
        set
        {
            if (value is Task)
                throw new InvalidCastException("MiddlewareContext Result can not be a Task.");
            _result = value!;
        }
    }

    private void ResetProps()
    {
        for (var i = 0; i < Args.Length; i++)
        {
            if (Args[i] == null)
                continue;

            if (Args[i]!.GetType().IsCancellationToken())
                Args[i] = CancellationToken;

            if (Args[i]!.GetType().IsStream())
                Args[i] = Stream;
        }
    }

    public override string ToString()
    {
        return $"[ActionExecutingContext]\r\nMethod:{InstanceMethod.MethodInfo.Name}\r\n\r\nHeader:\r\n{HeaderStr(Header)}\r\n{ArgsStr(PureArgs)}";
    }

    private static Type? GetFuncType(IEnumerable<object?> args)
    {
        foreach (var i in args)
        {
            if (i == null)
                continue;

            var t = i.GetType();
            if (t.IsFuncT())
                return t.GetGenericArguments()[0];
        }

        return null;
    }

    private static string ArgsStr(IEnumerable<object?> list)
    {
        var s = "";
        var i = 0;
        foreach (var p in list)
            s += $"Param:{i++}\r\n{p.ToDtoJson()}\r\n";
        return s;
    }

    private static string HeaderStr(Dictionary<string, object?> header)
    {
        var s = "";
        foreach (var p in header)
            s += $"  {p.Key}:{p.Value}\r\n";
        return s;
    }

    public virtual void OnSendResultStreamEndOrFault()
    {
        SendResultStreamEndOrFault?.Invoke(this, EventArgs.Empty);
    }

    public virtual void OnSendResultStreamStarted()
    {
        SendResultStreamStarted?.Invoke(this, EventArgs.Empty);
    }
}