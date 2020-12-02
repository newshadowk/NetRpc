using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataContract1
{
    public interface IService1
    {
        Task<Ret> Call(InParam p, int i, Stream stream, Func<int, Task> progs, CancellationToken token);
    }

    [Serializable]
    public class InParam
    {
        public string P1 { get; set; }

        public override string ToString()
        {
            return $"{nameof(P1)}: {P1}";
        }
    }

    [Serializable]
    public class Ret
    {
        public string P1 { get; set; }

        [field: NonSerialized]
        public Stream Stream { get; set; }
    }
}