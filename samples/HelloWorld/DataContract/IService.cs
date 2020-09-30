using System.Threading.Tasks;

namespace DataContract
{
    public interface IServiceAsync
    {
        Task CallAsync(string s);
    }
}