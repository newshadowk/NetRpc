using System;
using System.IO;
using NetRpc;

namespace DataContract
{
    public interface IService
    {
        void Hearbeat();

        [GrpcIgnore]
        void FilterAndHeader();

        CustomObj SetAndGetObj(CustomObj obj);

        void CallByCallBack(Action<CustomCallbackObj> cb);

        /// <exception cref="NotImplementedException"></exception>
        void CallBySystemException();

        /// <exception cref="CustomException"></exception>>
        void CallByCustomException();

        Stream GetStream();

        void SetStream(Stream data);

        Stream EchoStream(Stream data);

        ComplexStream GetComplexStream();

        ComplexStream ComplexCall(CustomObj obj, Stream data, Action<CustomCallbackObj> cb);
    }
}