using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetRpc;

namespace DataContract
{

    [Serializable]
    public class SendObj
    {
        public string P1 { get; set; }

        public int I1 { get; set; }

        public bool B1 { get; set; }
        
        public DateTime D1 { get; set; }

        public InnerObj InnerObj { get; set; }

        public List<InnerObj> BigList { get; set; }
    }

    [Serializable]
    public class InnerObj
    {
        public string IP1 { get; set; }
    }

    [Serializable]
    public class Result
    {
        public string P1 { get; set; } = "p1 value;";

        public string P2 { get; set; } = "p2 value;";
    }

    public interface IService
    {
        Task<Result> Call(string s);
    }

    [HttpRoute("ReService", true)]
    public interface IService_1
    {
        Task<Result> Call_1Async(SendObj s, int i1, bool b1, Action<int> cb, CancellationToken token);
    }

    public interface IService_1_1
    {
        Task<Result> Call_1_1(int i1);
    }

    public interface IService_2
    {
        Task<Result> Call_2(bool b);
    }
}