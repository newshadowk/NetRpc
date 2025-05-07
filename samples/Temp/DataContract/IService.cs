using System.Collections.Generic;
using System.Threading.Tasks;
using NetRpc.Contract;

namespace DataContract;

// public interface IServiceAsync
// {
//     [HttpGet]
//     Task<string> CallAsync(A1 a1);
// }
//
// public interface IService2Async
// {
//     [HttpGet]
//     Task<string> CallAsync(B1 b1);
// }

[Tag("全国院校")]
public interface ISchoolService
{
    /// <summary>
    /// [基础数据] 获取所有科类
    /// </summary>
    [HttpGet("/category/{id}/list")]
    Task<List<IdName>> GetAdmissionCategoryListAsync(int id);
}

public class IdName(int id, string name)
{
    public int Id { get; set; } = id;

    public string Name { get; set; } = name;

    public override string ToString()
    {
        return $" {Id}, {Name}";
    }
}

[Tag("专业数据")]
public interface IMajorService
{
    /// <summary>
    /// 获取所有的专业类别列表，包含三层
    /// </summary>
    [HttpGet("/major-category/{id}/list")]
    Task<List<IdName>> GetMajorCategoryListAsync(int id);
}

public class SchoolService : ISchoolService
{
    public Task<List<IdName>> GetAdmissionCategoryListAsync(int id)
    {
        throw new System.NotImplementedException();
    }
}

public class MajorService : IMajorService
{
    public Task<List<IdName>> GetMajorCategoryListAsync(int id)
    {
        throw new System.NotImplementedException();
    }
}