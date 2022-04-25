using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CSRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetRpc.Contract;
using NetRpc.Http.Client;

namespace NetRpc.Http.ShortConn;

public abstract class SCRedisHelper : RedisHelper<SCRedisHelper> {}

public class CacheHandler
{
    private readonly Cache _cache;
    private readonly CancelWatcher _cancelWatcher;
    private readonly MiddlewareBuilder _middlewareBuilder;
    private readonly HttpServiceOptions _options;
    private readonly ILogger<CacheHandler> _log;

    public CacheHandler(Cache cache, CancelWatcher cancelWatcher, MiddlewareBuilder middlewareBuilder, FilePrune filePrune, ILoggerFactory factory,
        IOptions<HttpServiceOptions> options)
    {
        _cache = cache;
        _cancelWatcher = cancelWatcher;
        _middlewareBuilder = middlewareBuilder;
        _options = options.Value;
        _log = factory.CreateLogger<CacheHandler>();
        filePrune.Start();
    }

    private static ActionExecutingContext GetContext(List<Instance> instances, IServiceProvider serviceProvider, ActionInfo action, Func<object?, Task> cb,
        ProxyStream? stream, object[] pureArgs, Dictionary<string, object?> header, CancellationToken token)
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

    public async Task<string> StartAsync<TServcie, TResult>(string methodName, Stream? stream, params object[] pureArgs) where TResult: class
    {
        ProxyStream? ps = null;
        if (stream != null)
        {
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            ps = new ProxyStream(ms);
        }

        return InnerStart<TResult>(typeof(TServcie).GetMethod(methodName)!.ToActionInfo(), ps, pureArgs, GlobalActionExecutingContext.Context!.Header);
    }

    private string InnerStart<T>(ActionInfo action, ProxyStream? stream, object[] pureArgs, Dictionary<string, object?> header) where T: class
    {
        var id = Guid.NewGuid().ToString("N");
        InnerStart<T>(id, action, stream, pureArgs, header);
        return id;
    }

    private async void InnerStart<T>(string id, ActionInfo action, ProxyStream? stream, object[] pureArgs, Dictionary<string, object?> header) where T : class
    {
        await _cache.CreateAsync<T>(id);

        async Task Cb(object? i)
        {
            await _cache.SetProgAsync<T>(id, i);
        }

        var contractOptions = GlobalServiceProvider.Provider!.GetRequiredService<IOptions<ContractOptions>>();
        using var scope = GlobalServiceProvider.Provider!.GetRequiredService<IServiceScopeFactory>().CreateScope();
        GlobalServiceProvider.ScopeProvider = scope.ServiceProvider;
        var instances = scope.ServiceProvider.GetContractInstances(contractOptions.Value);
        var context = GetContext(instances, scope.ServiceProvider, action, Cb, stream, pureArgs, header, _cancelWatcher.Create(id).Token);

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
            await _cache.SetFaultAsync<T>(id, e, context);
        }
    }

    public async Task<ContextData> GetProgressAsync(string id)
    {
        return (await _cache.GetAsync<object>(id)).Data;
    }

    public async Task<T?> GetResultAsync<T>(string id) where T : class
    {
        var c = await _cache.GetWithSteamAsync<T>(id);

        if (!_options.RemainShortConnCacheIn30MinutesWhenFinished)
        {
            //DelAsync
            if (c.Data.HasStream)
            {
                GlobalActionExecutingContext.Context!.SendResultStreamEndOrFault += (_, _) => { _cache.DelAsync(id); };
            }
            else
                await _cache.DelAsync(id);
        }

        return c.Result;
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

public class ShortConnRedis : IDisposable
{
    // ReSharper disable MemberCanBeMadeStatic.Global
    private readonly ILogger _log;
    private const int ExpireSeconds = 1800;
    private const string ChannelName = "task-cancel";
    private const string IdPrefixKey = "task_";
    private const string PruneLastTimeKey = "task_prune_last_time";
    private CSRedisClient.SubscribeObject? _subObj;

    public ShortConnRedis(IOptions<HttpServiceOptions> options, ILoggerFactory loggerFactory)
    {
        _log = loggerFactory.CreateLogger<ShortConnRedis>();
        var c = new CSRedisClient(options.Value.ShortConnRedisConnStr);
        SCRedisHelper.Initialization(c);
    }

    public void Subscribe(Action<CSRedisClient.SubscribeMessageEventArgs> action)
    {
        _subObj = SCRedisHelper.Subscribe((ChannelName, action));
    }

    public void Publish(string id)
    {
        SCRedisHelper.Publish(ChannelName, id);
    }

    public async Task SetPruneLastTimeAsync(DateTimeOffset dt)
    {
        await SCRedisHelper.SetAsync(PruneLastTimeKey, dt.ToUniversalTime().ToString("O"));
    }

    public async Task<DateTimeOffset> GetPruneLastTimeAsync()
    {
        if (!await SCRedisHelper.ExistsAsync(PruneLastTimeKey))
            await SetPruneLastTimeAsync(DateTimeOffset.Now);

        var s = await SCRedisHelper.GetAsync(PruneLastTimeKey);
        return DateTimeOffset.Parse(s);
    }

    public async Task SetAsync<T>(string id, InnerContextData<T> obj) where T : class
    {
        var ok = await SCRedisHelper.SetAsync($"{IdPrefixKey}{id}", obj, ExpireSeconds);
        if (!ok)
            _log.LogError($"SetAsync err, id:{id}, value:\r\n{obj.ToDtoJson()}");
    }

    public Task<InnerContextData<T>> GetAsync<T>(string id) where T : class
    {
        return SCRedisHelper.GetAsync<InnerContextData<T>>($"{IdPrefixKey}{id}");
    }

    public Task DelAsync(string id)
    {
        return SCRedisHelper.DelAsync(id);
    }

    public Task<bool> ExistsAsync(string id)
    {
        return SCRedisHelper.ExistsAsync($"{IdPrefixKey}{id}");
    }
    // ReSharper restore MemberCanBeMadeStatic.Global

    public void Dispose()
    {
        _subObj?.Dispose();
    }
}

public class Cache
{
    private readonly ShortConnRedis _redis;
    private readonly FileCache _fileCache;

    public Cache(ShortConnRedis redis, FileCache fileCache)
    {
        _redis = redis;
        _fileCache = fileCache;
    }

    public Task CreateAsync<T>(string id) where T : class
    {
        return _redis.SetAsync(id, new InnerContextData<T>());
    }

    public Task<InnerContextData<T>> GetAsync<T>(string id) where T : class
    {
        return _redis.GetAsync<T>(id);
    }

    public async Task<InnerContextData<T>> GetWithSteamAsync<T>(string id) where T : class
    {
        var c = await _redis.GetAsync<T>(id);
        if (c.Data.HasStream)
            c.Result.SetStream(_fileCache.OpenRead(id));
        return c;
    }

    public async Task SetProgAsync<T>(string id, object? prog) where T : class
    {
        var d = await _redis.GetAsync<T>(id);
        d.Data.Prog = prog.ToDtoJsonNotIndented();
        await _redis.SetAsync(id, d);
    }

    public async Task SetResultAsync<T>(string id, T? result) where T : class
    {
        var d = await _redis.GetAsync<T>(id);
        if (result.TryGetStream(out var retStream, out var retStreamName))
        {
            d.Data.HasStream = true;
            d.Data.StreamName = retStreamName;
            await _fileCache.WriteAsync(id, retStream);
        }

        d.Result = result;
        d.Data.StatusCode = 200;
        d.Data.Status = ContextStatus.End;
        await _redis.SetAsync(id, d);
    }

    public Task DelAsync(string id)
    {
        _fileCache.Del(id);
        return _redis.DelAsync(id);
    }

    public async Task SetFaultAsync<T>(string id, Exception e, ActionExecutingContext? context) where T : class
    {
        var d = await GetAsync<T>(id);

        // UnWarp FaultException
        e = NetRpc.Helper.UnWarpException(e);

        d.Data.Status = ContextStatus.Err;
        if (e.GetExceptionFrom<OperationCanceledException>(true) != null)
        {
            d.Data.StatusCode = ClientConst.CancelStatusCode;
            d.Data.ErrMsg = e.Message;
            await _redis.SetAsync(id, d);
            return;
        }

        var textEx = e.GetExceptionFrom<ResponseTextException>();
        if (textEx != null)
        {
            d.Data.StatusCode = textEx.StatusCode;
            d.Data.ErrMsg = textEx.Text;
            await _redis.SetAsync(id, d);
            return;
        }

        var t = context?.ContractMethod.FaultExceptionAttributes.FirstOrDefault(i => e.GetExceptionFrom(i.DetailType) != null);
        if (t != null)
        {
            d.Data.ErrCode = t.ErrorCode;
            d.Data.StatusCode = t.StatusCode;
            d.Data.ErrMsg = t.Description ?? e.Message;
            await _redis.SetAsync(id, d);
            return;
        }

        d.Data.StatusCode = ClientConst.DefaultExceptionStatusCode;
        d.Data.ErrMsg = e.Message;
        await _redis.SetAsync(id, d);
    }
}

public class FileCache
{
    private readonly string _shortConnTempDir;
    private readonly ILogger<FileCache> _log;

    public FileCache(IOptions<HttpServiceOptions> options, ILoggerFactory factory)
    {
        if (options.Value.ShortConnTempDir is null)
            throw new ArgumentNullException(nameof(options.Value.ShortConnTempDir));

        _shortConnTempDir = options.Value.ShortConnTempDir;
        _log = factory.CreateLogger<FileCache>();
    }

    public string[] GetFiles()
    {
        return Directory.GetFiles(_shortConnTempDir);
    }

    public async Task WriteAsync(string id, Stream stream)
    {
        var path = GetPath(id);
        var dir = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        await using var fw = File.OpenWrite(path);
        await stream.CopyToAsync(fw);
    }

    public void Del(string id)
    {
        var path = GetPath(id);
        try
        {
            File.Delete(GetPath(id));
        }
        catch (Exception e)
        {
            _log.LogError(e, $"Del err. path:{path}");
        }
    }

    public Stream OpenRead(string id)
    {
        var path = GetPath(id);
        return File.OpenRead(path);
    }

    private string GetPath(string id)
    {
        return Path.Combine(_shortConnTempDir, id);
    }
}

public class FilePrune
{
    private readonly TimeSpan _checkTimeSpan = TimeSpan.FromMinutes(10);
    private readonly ShortConnRedis _redis;
    private readonly FileCache _fileCache;
    private readonly BusyTimer _t;
    private readonly ILogger<FilePrune> _log;

    public FilePrune(ShortConnRedis redis, FileCache fileCache, ILoggerFactory factory)
    {
        _log = factory.CreateLogger<FilePrune>();
        _redis = redis;
        _fileCache = fileCache;
        _t = new BusyTimer(_checkTimeSpan.TotalMilliseconds);
        _t.ElapsedAsync += TElapsedAsync;
    }

    private async Task TElapsedAsync(object sender, ElapsedEventArgs e)
    {
        if (DateTimeOffset.Now - await _redis.GetPruneLastTimeAsync() > _checkTimeSpan)
        {
            await PruneAsync();
            await _redis.SetPruneLastTimeAsync(DateTimeOffset.Now);
        }
    }

    public void Start()
    {
        _t.Start();
    }

    public async Task PruneAsync()
    {
        foreach (var file in _fileCache.GetFiles())
        {
            if (!await _redis.ExistsAsync(Path.GetFileName(file)))
            {
                _log.LogInformation($"del start, {file}");
                _fileCache.Del(file);
                _log.LogInformation("del end.");
            }
        }
    }
}