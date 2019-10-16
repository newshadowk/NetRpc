using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetRpc
{
    internal sealed class ServiceOnceTransfer
    {
        private readonly IServiceOnceApiConvert _convert;
        private readonly List<Instance> _instances;
        private readonly IServiceProvider _serviceProvider;
        private readonly MiddlewareBuilder _middlewareBuilder;
        private readonly IGlobalServiceContextAccessor _globalServiceContextAccessor;
        private readonly ChannelType _channelType;
        private readonly CancellationTokenSource _serviceCts = new CancellationTokenSource();

        public ServiceOnceTransfer(List<Instance> instances, IServiceProvider serviceProvider, IServiceOnceApiConvert convert,
            MiddlewareBuilder middlewareBuilder, IGlobalServiceContextAccessor globalServiceContextAccessor, ChannelType channelType)
        {
            _instances = instances;
            _serviceProvider = serviceProvider;
            _middlewareBuilder = middlewareBuilder;
            _globalServiceContextAccessor = globalServiceContextAccessor;
            _channelType = channelType;
            _convert = convert;
        }

        public async Task StartAsync()
        {
            await _convert.StartAsync(_serviceCts);
        }

        public async Task HandleRequestAsync()
        {
            object ret;
            ServiceContext context = null;
            ServiceCallParam scp = null;

            try
            {
                //get context
                scp = await GetServiceCallParamAsync();
                context = ApiWrapper.GetServiceContext(scp, _instances, _serviceProvider, _channelType);

                //set Accessor
                _globalServiceContextAccessor.Context = context;

                //CheckIgnore
                CheckIgnore(context);

                //middleware Invoke
                ret = await _middlewareBuilder.InvokeAsync(context);

                //if Post, do not need send back to client.
                if (scp.Action.IsPost)
                    return;
            }
            catch (Exception e)
            {
                //if Post, do not need send back to client.
                if (scp != null && scp.Action.IsPost)
                    return;

                //send fault
                await _convert.SendFaultAsync(e, context);
                return;
            }

            var hasStream = ret.TryGetStream(out var retStream, out var retStreamName);

            //send result
            var sendStreamNext = await _convert.SendResultAsync(new CustomResult(ret, hasStream, retStream.GetLength()), retStream, retStreamName, context);
            if (!sendStreamNext)
                return;

            //send stream
            await SendStreamAsync(hasStream, retStream, scp);
        }

        private static void CheckIgnore(ServiceContext context)
        {
            switch (context.ChannelType)
            {
                case ChannelType.Undefined:
                    break;
                case ChannelType.Grpc:
                    if (context.MethodObj.GrpcIgnore)
                        throw new NetRpcIgnoreException("Grpc");
                    break;
                case ChannelType.RabbitMQ:
                    if (context.MethodObj.RabbitMQIgnore)
                        throw new NetRpcIgnoreException("RabbitMQ");
                    break;
                case ChannelType.Http:
                    if (context.MethodObj.HttpIgnore)
                        throw new NetRpcIgnoreException("Http");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task SendStreamAsync(bool hasStream, Stream retStream, ServiceCallParam scp)
        {
            if (hasStream)
            {
                try
                {
                    using (retStream)
                    {
                        await Helper.SendStreamAsync(i => _convert.SendBufferAsync(i), () =>
                            _convert.SendBufferEndAsync(), retStream, scp.Token);
                    }
                }
                catch (TaskCanceledException)
                {
                    await _convert.SendBufferCancelAsync();
                }
                catch (Exception)
                {
                    await _convert.SendBufferFaultAsync();
                }
            }
        }

        private async Task<ServiceCallParam> GetServiceCallParamAsync()
        {
            //onceCallParam
            var onceCallParam = await _convert.GetOnceCallParamAsync();

            //stream
            Stream stream;
            if (onceCallParam.Action.IsPost)
                stream = BytesToStream(onceCallParam.PostStream);
            else
                stream = _convert.GetRequestStream(onceCallParam.StreamLength);

            //serviceCallParam
            return new ServiceCallParam(onceCallParam,
                async i => await _convert.SendCallbackAsync(i),
                _serviceCts.Token, stream);
        }

        private static Stream BytesToStream(byte[] bytes)
        {
            if (bytes == null)
                return null;
            Stream stream = new MemoryStream(bytes);
            return stream;
        }
    }
}