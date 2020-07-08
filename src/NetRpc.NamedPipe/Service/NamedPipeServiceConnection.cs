using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace NetRpc.NamedPipe
{
//    internal sealed class NamedPipeServiceConnection : IServiceConnection
//    {
//        public void Dispose()
//        {
//            throw new NotImplementedException();
//        }

//        public event AsyncEventHandler<EventArgsT<byte[]>> ReceivedAsync;

//        public Task SendAsync(byte[] buffer)
//        {
//            throw new NotImplementedException();
//        }

//        public Task StartAsync()
//        {
//            throw new NotImplementedException();
//        }
//    }


//    internal sealed class NamedPipeServerProxy :
//#if NETSTANDARD2_1
//        IDisposableAsync
//#else
//        IDisposable
//#endif
//    {
//        private readonly NamedPipeServerStream _pipeServer;

//        public NamedPipeServerProxy(int maxNumberOfServerInstances, string name)
//        {
//            _pipeServer = new NamedPipeServerStream(name, PipeDirection.In, maxNumberOfServerInstances, PipeTransmissionMode.Message);
//        }

//        public event AsyncEventHandler<EventArgsT<byte[]>> ReceivedAsync;

//        //public async Task Send(byte[] buffer)
//        //{
//        //    _pipeServer.WriteAsync(buffer)
//        //}

//        public async void StartWaitForConnection()
//        {
//            var ms = new MemoryStream();
//            await _pipeServer.WaitForConnectionAsync();
//            var buffer = new byte[Helper.StreamBufferSize];
//            var readCount = await _pipeServer.ReadAsync(buffer, 0, Helper.StreamBufferSize);
//            while (_pipeServer.IsConnected && !_pipeServer.IsMessageComplete && readCount > 0)
//                ms.Write(buffer, 0, readCount);
//            await OnReceivedAsync(new EventArgsT<byte[]>(ms.ToArray()));
//        }

//        private Task OnReceivedAsync(EventArgsT<byte[]> e)
//        {
//            return ReceivedAsync.InvokeAsync(this, e);
//        }

//#if NETSTANDARD2_1
//        public async Task DisposeAsync()
//        {
//            if (_pipeServer == null)
//                return;
//            await _pipeServer.DisposeAsync();
//        }
//#endif

//        public void Dispose()
//        {
//            _pipeServer?.Dispose();
//        }
//    }

}