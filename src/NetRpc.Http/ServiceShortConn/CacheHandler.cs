using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetRpc.Contract;
using NetRpc.Http.Client;

namespace NetRpc.Http;

public class ShortConnCacheHandler
{
    private readonly ShortConnCache _cache;
    private readonly CancelWatcher _cancelWatcher;
    private readonly MiddlewareBuilder _middlewareBuilder;
    private readonly ILogger<ShortConnCacheHandler> _log;

    public ShortConnCacheHandler(ShortConnCache cache, CancelWatcher cancelWatcher, MiddlewareBuilder middlewareBuilder, ILoggerFactory factory)
    {
        _cache = cache;
        _cancelWatcher = cancelWatcher;
        _middlewareBuilder = middlewareBuilder;
        _log = factory.CreateLogger<ShortConnCacheHandler>();
    }

    private static ActionExecutingContext GetContext(List<Instance> instances, IServiceProvider serviceProvider, ActionInfo action, Func<object?, Task> cb,
        ProxyStream stream, object[] pureArgs, Dictionary<string, object?> header, CancellationToken token)
    {
        var (instanceMethodInfo, contractMethod, instance) = ApiWrapper.GetMethodInfo(action, instances);

        //get parameters
        var parameters = contractMethod.MethodInfo.GetParameters();

        //args
        var args = ApiWrapper.GetArgs(parameters, pureArgs, cb, token, stream);

        return new ActionExecutingContext(
            serviceProvider,
            header,
            instance,
            instanceMethodInfo,
            contractMethod,
            args,
            pureArgs,
            action,
            stream,
            instance.Contract,
            ChannelType.Http,
            cb,
            token);
    }

    public async void Start(string id, ActionInfo action, ProxyStream stream, object[] pureArgs, Dictionary<string, object?> header)
    {
        await _cache.CreateAsync(id);

        async Task Cb(object? i)
        {
            await _cache.SetProgAsync(id, i);
        }

        var contractOptions = GlobalServiceProvider.Provider!.GetRequiredService<IOptions<ContractOptions>>();
        var instances = GlobalServiceProvider.ScopeProvider!.GetContractInstances(contractOptions.Value);
        var context = GetContext(instances, GlobalServiceProvider.ScopeProvider!, action, Cb, stream, pureArgs, header, _cancelWatcher.Create(id).Token);

        try
        {
            var ret = await _middlewareBuilder.InvokeAsync(context);
            _cancelWatcher.Remove(id);
            await _cache.SetResultAsync(id, ret);
        }
        catch (Exception e)
        {
            _log.LogError(e, $"start err, id:{id}");
            _cancelWatcher.Remove(id);
            await _cache.SetFaultAsync(id, e, context);
        }
    }
    
    public async Task<ContextData> GetProgressAsync(string id)
    {
        return (await _cache.GetAsync(id)).Data;
    }

    public async Task<object?> GetResultAsync(string id)
    {
        var c = await _cache.GetWithSteamAsync(id);
        await _cache.DelAsync(id);
        return c.ResultWithOutStream;
    }

    public void Cancel(string id)
    {
        _cancelWatcher.Cancel(id);
    }
}

public class CancelWatcher
{
    private readonly ShortConnRedis _redis;
    private readonly Dictionary<string, CancellationTokenSource> _cancelDic = new();
    private readonly object _lockDic = new();

    public CancelWatcher(ShortConnRedis redis)
    {
        _redis = redis;
        _redis.Subscribe(msg => TryCancel(msg.Body));
    }

    public void Cancel(string id)
    {
        _redis.Publish(id);
    }

    public CancellationTokenSource Create(string id)
    {
        CancellationTokenSource cts = new();
        lock (_lockDic)
            _cancelDic.Add(id, cts);
        return cts;
    }

    public void Remove(string id)
    {
        lock (_lockDic)
            _cancelDic.Remove(id);
    }

    private void TryCancel(string id)
    {
        lock (_lockDic)
            if (_cancelDic.TryGetValue(id, out var v))
                v.Cancel();
    }
}

public class ShortConnRedis
{
    private readonly ILogger _log;
    private const int ExpireSeconds = 3600;
    private const string ChannelName = "task-cancel";

    public ShortConnRedis(IOptions<HttpServiceOptions> options, ILoggerFactory loggerFactory)
    {
        _log = loggerFactory.CreateLogger<ShortConnRedis>();
        var c = new CSRedisClient(options.Value.ShortConnRedisConnStr);
        var serializer = new BinaryCacheSerializer();
        c.CurrentSerialize = o => serializer.Serialize(o);
        c.CurrentDeserialize = (s, type) => serializer.Deserialize(s, type);
        RedisHelper.Initialization(c);
    }

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public void Subscribe(Action<CSRedisClient.SubscribeMessageEventArgs> action)
    {
        RedisHelper.Subscribe((ChannelName, action));
    }

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public void Publish(string id)
    {
        RedisHelper.Publish(ChannelName, id);
    }

    public async Task SetAsync(string id, InnerContextData obj)
    {
        var ok = await RedisHelper.SetAsync(id, obj, ExpireSeconds);
        if (!ok)
            _log.LogError($"SetAsync err, id:{id}, value:\r\n{obj.ToDtoJson()}");
    }

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public Task<InnerContextData> GetAsync(string id)
    {
        return RedisHelper.GetAsync<InnerContextData>(id);
    }

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public Task DelAsync(string id)
    {
        return RedisHelper.DelAsync(id);
    }
}

public class ShortConnCache
{
    private readonly ShortConnRedis _redis;
    private readonly ShortConnStreamCache _streamCache;

    public ShortConnCache(ShortConnRedis redis, ShortConnStreamCache streamCache)
    {
        _redis = redis;
        _streamCache = streamCache;
    }

    public Task CreateAsync(string id)
    {
        return _redis.SetAsync(id, new InnerContextData());
    }

    public Task<InnerContextData> GetAsync(string id)
    {
        return _redis.GetAsync(id);
    }

    public async Task<InnerContextData> GetWithSteamAsync(string id)
    {
        var c = await _redis.GetAsync(id);
        if (c.Data.HasStream) 
            c.ResultWithOutStream.SetStream(_streamCache.OpenRead(id));
        return c;
    }

    public async Task SetProgAsync(string id, object? prog)
    {
        var d = await _redis.GetAsync(id);
        d.Data.Prog = prog.ToDtoJson();
        await _redis.SetAsync(id, d);
    }

    public async Task SetResultAsync(string id, object? result)
    {
        var d = await _redis.GetAsync(id);
        if (result.TryGetStream(out var retStream, out var retStreamName))
        {
            d.Data.HasStream = true;
            d.Data.StreamName = retStreamName;

            await _streamCache.WriteAsync(id, retStream);
        }

        d.ResultWithOutStream = result;
        d.Data.StatusCode = 200;
        d.Data.Status = ContextStatus.End;
        await _redis.SetAsync(id, d);
    }

    public Task DelAsync(string id)
    {
        return _redis.DelAsync(id);
    }

    public async Task SetFaultAsync(string id, Exception e, ActionExecutingContext? context)
    {
        var d = await GetAsync(id);

        // UnWarp FaultException
        e = NetRpc.Helper.UnWarpException(e);

        d.Data.Status = ContextStatus.Err;

        if (e is OperationCanceledException)
        {
            d.Data.StatusCode = ClientConstValue.CancelStatusCode;
            d.Data.ErrMsg = e.Message;
            await _redis.SetAsync(id, d);
            return;
        }

        if (e is ResponseTextException textEx)
        {
            d.Data.StatusCode = textEx.StatusCode;
            d.Data.ErrMsg = textEx.Text;
            await _redis.SetAsync(id, d);
            return;
        }

        var t = context?.ContractMethod.FaultExceptionAttributes.FirstOrDefault(i => e.GetType() == i.DetailType);
        if (t != null)
        {
            d.Data.ErrCode = t.ErrorCode;
            d.Data.StatusCode = t.StatusCode;
            d.Data.ErrMsg = t.Description ?? e.Message;
            await _redis.SetAsync(id, d);
            return;
        }

        d.Data.StatusCode = ClientConstValue.DefaultExceptionStatusCode;
        d.Data.ErrMsg = e.Message;
        await _redis.SetAsync(id, d);
    }
}

public class ShortConnStreamCache
{
    private readonly string _shortConnTempDir;

    public ShortConnStreamCache(IOptions<HttpServiceOptions> options)
    {
        if (options.Value.ShortConnTempDir is null)
            throw new ArgumentNullException(nameof(options.Value.ShortConnTempDir));

        _shortConnTempDir = options.Value.ShortConnTempDir;
    }

    public async Task WriteAsync(string id, Stream stream)
    {
        var path = GetPath(id);
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        await using var fw = File.OpenWrite(path);
        await stream.CopyToAsync(fw);
    }

    public Stream OpenRead(string id)
    {
        var path = GetPath(id);
        return File.OpenRead(path);
    }

    private string GetPath(string id)
    {
        string path1 = _shortConnTempDir;
        DateTime today = DateTime.Today;
        string path2 = today.ToString("yyyyMM");
        today = DateTime.Today;
        string path3 = today.ToString("dd");
        string path = Path.Combine(path1, path2, path3, id);
        return path;
    }
}