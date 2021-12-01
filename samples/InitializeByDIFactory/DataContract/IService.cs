using System.Threading.Tasks;

namespace DataContract;

public interface IService
{
    Task CallAsync(string s);
}