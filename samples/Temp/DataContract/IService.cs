using System;
using System.IO;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IService
    {
        Task Call3(Stream stream, int index, Action<double> prog);

        Task Call();
    }
}