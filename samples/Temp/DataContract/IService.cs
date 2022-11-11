using System.IO;
using System.Threading.Tasks;

namespace DataContract;

public interface IServiceAsync
{
    Task<string> CallAsync(string s);

    Task<string> Call2Async(Stream s);
}