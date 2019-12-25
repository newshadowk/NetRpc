using System.IO;
using System.Threading.Tasks;

namespace DataContract
{
    public interface IService
    {
        Task Call(string s);

        Task<Stream> Echo(Stream s);
    }
}