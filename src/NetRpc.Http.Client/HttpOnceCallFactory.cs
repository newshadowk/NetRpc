using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NetRpc.Http.Client;

internal sealed class HttpOnceCallFactory : IOnceCallFactory
{
    private readonly HttpClientOptions _options;
    private readonly ILogger _logger;
    private HubConnection? _connection;
    private HubCallBackNotifier? _notifier;
    private volatile string? _connId;
    private readonly AsyncLock _lockInit = new();

    public HttpOnceCallFactory(IOptions<HttpClientOptions> options, ILoggerFactory factory)
    {
        _options = options.Value;
        _logger = factory.CreateLogger("NetRpc");
    }

    private async Task<string> InitConnectionAsync()
    {
        if (_connection == null)
        {
            using (await _lockInit.LockAsync())
            {
                if (_connection == null)
                {
                    if (_options.SignalRHubUrl != null)
                    {
                        _connection = new HubConnectionBuilder()
                            .WithUrl(_options.SignalRHubUrl)
                            .Build();
                        _notifier = new HubCallBackNotifier(_connection);
                        _connection.On<string, string>("Callback", HubConnection_Callback);
                    }
                }
            }
        }

        // ReSharper disable once PossibleNullReferenceException
        if (_connection!.State == HubConnectionState.Disconnected)
        {
            using (await _lockInit.LockAsync())
            {
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    try
                    {
                        await _connection.StartAsync();
                        _connId = await _connection.InvokeAsync<string>("GetConnId");
                        return _connId;
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(e, "_connection.StartAsync() failed.");
                    }
                }
            }
        }

        return _connId!;
    }

    private void HubConnection_Callback(string callId, string data)
    {
        _notifier?.OnCallback(new CallbackEventArgs(callId, data));
    }

    public async Task<IOnceCall> CreateAsync(int timeoutInterval, bool isRetry)
    {
        var cid = await InitConnectionAsync();
        var convert = new HttpClientOnceApiConvert(_options.ApiUrl, cid, _notifier!, timeoutInterval);
        return new OnceCall(convert, timeoutInterval, _logger);
    }

    public void Dispose()
    {
        _connection?.StopAsync();
    }
}