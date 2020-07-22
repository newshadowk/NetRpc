using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NetRpc.Http.Client
{
    internal sealed class HttpOnceCallFactory : IOnceCallFactory
    {
        private readonly HttpClientOptions _options;
        private readonly ILogger _logger;
        private HubConnection _connection;
        private HubCallBackNotifier _notifier;
        private volatile string _connectionId;
        private readonly AsyncLock _lockInit = new AsyncLock();

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
#if NETCOREAPP2_1
            try
            {
                await _connection.StartAsync();
                _connectionId = await _connection.InvokeAsync<string>("GetConnectionId");
                return _connectionId;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "_connection.StartAsync() failed.");
            }
#else
            // ReSharper disable once PossibleNullReferenceException
            if (_connection.State == HubConnectionState.Disconnected)
            {
                using (await _lockInit.LockAsync())
                {
                    if (_connection.State == HubConnectionState.Disconnected)
                    {
                        try
                        {
                            await _connection.StartAsync();
                            _connectionId = await _connection.InvokeAsync<string>("GetConnectionId");
                            return _connectionId;
                        }
                        catch (Exception e)
                        {
                            _logger.LogWarning(e, "_connection.StartAsync() failed.");
                        }
                    }
                }
            }
#endif
            return _connectionId;
        }

        private void HubConnection_Callback(string callId, string data)
        {
            _notifier.OnCallback(new CallbackEventArgs(callId, data));
        }

        public void Dispose()
        {
            //have not deadlock issue.
            _connection.StopAsync().Wait();
        }

#if NETSTANDARD2_1 || NETCOREAPP3_1
        public async ValueTask DisposeAsync()
        {
            await _connection.StopAsync();
        }
#endif

        public async Task<IOnceCall> CreateAsync(int timeoutInterval)
        {
            var cid = await InitConnectionAsync();
            var convert = new HttpClientOnceApiConvert(_options.ApiUrl, cid, _notifier, timeoutInterval);
            return new OnceCall(convert, timeoutInterval, _logger);
        }
    }
}