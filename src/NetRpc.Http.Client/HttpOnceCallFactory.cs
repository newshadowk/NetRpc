using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace NetRpc.Http.Client
{
    internal sealed class HttpOnceCallFactory : IOnceCallFactory
    {
        private readonly HttpClientOptions _options;
        private HubConnection _connection;
        private HubCallBackNotifier _notifier;
        private volatile string _connectionId;
        private readonly AsyncLock _lockInit = new AsyncLock();

        public HttpOnceCallFactory(HttpClientOptions options)
        {
            _options = options;
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
                            Debug.WriteLine("_connection.StartAsync() failed. " + e.Message);
                        }
                    }
                }
            }

            return _connectionId;
        }

        private void HubConnection_Callback(string callId, string data)
        {
            _notifier.OnCallback(new CallbackEventArgs(callId, data));
        }

        public void Dispose()
        {
            _connection.StopAsync().Wait();
        }

#if NETSTANDARD2_1
        public async ValueTask DisposeAsync()
        {
            await _connection.StopAsync();
        }
#endif

        public async Task<IOnceCall> CreateAsync(int timeoutInterval)
        {
            var cid = await InitConnectionAsync();
            var convert = new HttpClientOnceApiConvert(_options.ApiUrl, cid, _notifier, timeoutInterval);
            return new OnceCall(convert, timeoutInterval);
        }
    }
}