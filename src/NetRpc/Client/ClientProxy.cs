using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Options;

namespace NetRpc
{
    public class ClientProxy<TService> : IClientProxy<TService>
    {
        private bool _disposed;
        private readonly object _lockDispose = new object();
        public event EventHandler Connected;
        public event EventHandler DisConnected;
        public event EventHandler<EventArgsT<Exception>> ExceptionInvoked;
        private readonly object _lockObj = new object();
        private readonly IOnceCallFactory _factory;
        private bool _isConnected;
        public event Func<IClientProxy<TService>, Task> Heartbeat;
        private readonly Timer _tHearbeat;
        private readonly Call _call;

        public Dictionary<string, object> AdditionHeader
        {
            get => _call.AdditionHeader;
            set => _call.AdditionHeader = value;
        }

        public TService Proxy { get; }

        public ClientProxy(IOnceCallFactory factory, IOptionsMonitor<NetRpcClientOption> options, IServiceProvider serviceProvider, string optionsName = null)
        {
            _factory = factory;
            _call = new Call(serviceProvider, new ContractInfo(typeof(TService)), factory, options.CurrentValue.TimeoutInterval, optionsName);
            var invoker = new ClientMethodInvoker(_call);
            Proxy = SimpleDispatchProxyAsync.Create<TService>(invoker);
            ((SimpleDispatchProxyAsync)(object)Proxy).ExceptionInvoked += ProxyExceptionInvoked;
            _tHearbeat = new Timer(options.CurrentValue.HearbeatInterval);
            _tHearbeat.Elapsed += THearbeatElapsed;

            options.OnChange(i =>
            {
                _call.Config(i.TimeoutInterval);
                _tHearbeat.Interval = i.HearbeatInterval;
            });
        }

        public ClientProxy(IClientConnectionFactory factory, IOptionsMonitor<NetRpcClientOption> options, IServiceProvider serviceProvider, string optionsName = null)
            : this(new OnceCallFactory(factory), options, serviceProvider, optionsName)
        {
        }

        private void ProxyExceptionInvoked(object sender, EventArgsT<Exception> e)
        {
            OnExceptionInvoked(e);
        }

        private void THearbeatElapsed(object sender, ElapsedEventArgs e)
        {
            DoHeartbeat();
        }

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
                HeartbeatAsync();
#pragma warning restore 4014
        }

        public async Task HeartbeatAsync()
        {
            await OnHeartbeat();
            IsConnected = true;
        }

        private async void DoHeartbeat()
        {
            try
            {
                await OnHeartbeat();
                IsConnected = true;
            }
            catch
            {
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

        private Task OnHeartbeat()
        {
            var func = Heartbeat;
            if (func != null)
                return func(this);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ClientProxy()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (_lockDispose)
            {
                if (_disposed)
                    return;

                if (disposing)
                    DisposeManaged();

                _disposed = true;
            }
        }

        private void DisposeManaged()
        {
            _tHearbeat?.Dispose();
            _factory.Dispose();
        }

        private void OnExceptionInvoked(EventArgsT<Exception> e)
        {
            ExceptionInvoked?.Invoke(this, e);
        }
    }
}