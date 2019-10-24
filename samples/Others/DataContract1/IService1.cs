using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataContract1
{
    public interface IService1
    {
        Task<Ret> Call(InParam p, Stream stream, Action<int> progs, CancellationToken token);
    }

    public class InParam
    {
        public string P1 { get; set; }

        public override string ToString()
        {
            return $"{nameof(P1)}: {P1}";
        }
    }

    public class Ret
    {
        public string P1 { get; set; }

        public Stream Stream { get; set; }
    }
}