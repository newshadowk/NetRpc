using Microsoft.AspNetCore.SignalR.Client;

namespace NetRpc.Http.Client
{
    internal sealed class HttpOnceCallFactory : IOnceCallFactory
    {
        private readonly HttpClientOptions _options;
        private HubConnection _connection;
        private HubCallBackNotifier _notifier;
        private volatile string _connectionId;
        private readonly object _lockInit = new object();

        public HttpOnceCallFactory(HttpClientOptions options)
        {
            _options = options;
        }

        private string InitConnection()
        {
            if (_connection == null)
            {
                lock (_lockInit)
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
                lock (_options)
                {
                    if (_connection.State == HubConnectionState.Disconnected)
                    {
                        try
                        {
                            _connection.StartAsync().Wait();
                            _connectionId = _connection.InvokeAsync<string>("GetConnectionId").Result;
                            return _connectionId;
                        }
                        catch
                        {
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

        public IOnceCall<T> Create<T>(ContractInfo contract, int timeoutInterval)
        {
            var cid = InitConnection();
            var convert = new HttpClientOnceApiConvert(contract, _options.ApiUrl, cid, _notifier, timeoutInterval);
            return new OnceCall<T>(convert, timeoutInterval);
        }
    }
}