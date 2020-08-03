using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NetRpc
{
    public class ClientProxy<TService> : IClientProxy<TService> where TService : class
    {
        private bool _disposed;
        private readonly AsyncLock _lockDispose = new AsyncLock();
        public event EventHandler? Connected;
        public event EventHandler? DisConnected;
        public event EventHandler<EventArgsT<Exception>>? ExceptionInvoked;
        public event AsyncEventHandler? HeartbeatAsync;
        private readonly object _lockObj = new object();
        private readonly IOnceCallFactory _factory;
        private readonly ILogger _logger;
        private bool _isConnected;
        private readonly Timer _tHearbeat;
        private readonly Call _call;
        public Guid Id { get; } = Guid.NewGuid();

        public Dictionary<string, object?> AdditionHeader
        {
            get => _call.AdditionHeader;
            set => _call.AdditionHeader = value;
        }

        public ClientProxy(IOnceCallFactory factory,
            IOptions<NClientOption> nClientOptions,
            IOptions<ClientMiddlewareOptions> clientMiddlewareOptions,
            IActionExecutingContextAccessor actionExecutingContextAccessor,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            string? optionsName = null)
        {
            _factory = factory;
            _logger = loggerFactory.CreateLogger("NetRpc");

            _call = new Call(Id,
                serviceProvider,
                clientMiddlewareOptions.Value,
                actionExecutingContextAccessor,
                new ContractInfo(typeof(TService)),
                factory,
                nClientOptions.Value.TimeoutInterval,
                nClientOptions.Value.ForwardHeader, optionsName);

            var invoker = new ClientMethodInvoker(_call);
            Proxy = SimpleDispatchProxyAsync.Create<TService>(invoker);
            ((SimpleDispatchProxyAsync) (object) Proxy).ExceptionInvoked += ProxyExceptionInvoked;
            _tHearbeat = new Timer(nClientOptions.Value.HearbeatInterval);
            _tHearbeat.Elapsed += THearbeatElapsed;
        }

        public ClientProxy(IClientConnectionFactory factory,
            IOptions<NClientOption> nClientOptions,
            IOptions<ClientMiddlewareOptions> clientMiddlewareOptions,
            IActionExecutingContextAccessor actionExecutingContextAccessor,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            string? optionsName = null)
            : this(new OnceCallFactory(factory, loggerFactory),
                nClientOptions,
                clientMiddlewareOptions,
                actionExecutingContextAccessor,
                serviceProvider,
                loggerFactory,
                optionsName)
        {
        }

        private void ProxyExceptionInvoked(object? sender, EventArgsT<Exception> e)
        {
            OnExceptionInvoked(e);
        }

        private void THearbeatElapsed(object sender, ElapsedEventArgs e)
        {
            DoHeartbeat();
        }

        public TService Proxy { get; }

        object IClientProxy.Proxy => Proxy;

        public bool IsConnected
        {
            get
            {
                lock (_lockObj)
                    return _isConnected;
            }
            protected set
            {
                lock (_lockObj)
                {
                    if (_isConnected == value)
                        return;
                    _isConnected = value;
                }

                if (value)
                    OnConnected();
                else
                    OnDisConnected();
            }
        }

        public void StartHeartbeat(bool isImmediate = false)
        {
            _tHearbeat.Start();
            if (isImmediate)
#pragma warning disable 4014
                InvokeHeartbeatAsync();
#pragma warning restore 4014
        }

        public void StopHeartBeat()
        {
            _tHearbeat.Stop();
        }

        public async Task InvokeHeartbeatAsync()
        {
            await OnHeartbeatAsync();
            IsConnected = true;
        }

        private async void DoHeartbeat()
        {
            try
            {
                await OnHeartbeatAsync();
                IsConnected = true;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, null);
            }
        }

        private void OnConnected()
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }

        protected void OnDisConnected()
        {
            DisConnected?.Invoke(this, EventArgs.Empty);
        }

        private Task OnHeartbeatAsync()
        {
            return HeartbeatAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        ~ClientProxy()
        {
#pragma warning disable 4014
            DisposeAsync(false);
#pragma warning restore 4014
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            using (await _lockDispose.LockAsync())
            {
                if (_disposed)
                    return;

                if (disposing)
                    await DisposeManagedAsync();

                _disposed = true;
            }
        }

        private async ValueTask DisposeManagedAsync()
        {
            _tHearbeat?.Dispose();
            await _factory.DisposeAsync();
        }

        private void OnExceptionInvoked(EventArgsT<Exception> e)
        {
            ExceptionInvoked?.Invoke(this, e);
        }
    }
}