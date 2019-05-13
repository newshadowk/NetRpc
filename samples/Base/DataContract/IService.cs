﻿using System;
using System.IO;

namespace DataContract
{
    public interface IService
    {
        void FilterAndHeader();

        T2 CallByGeneric<T1, T2>(T1 obj);

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