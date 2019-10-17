using System;
using System.IO;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IService
    {
        Task Call(Stream stream, Action<int> prog);

        Task Call2(Stream stream);

        Task<Stream> Call3(Stream stream, Action<int> prog);

        Task Call4();
    }

    public interface IService2
    {
        Task Call();
    }

    public interface IService3
    {
        Task Call();
    }
}