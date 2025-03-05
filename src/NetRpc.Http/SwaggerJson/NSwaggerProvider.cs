using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NetRpc.Contract;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NetRpc.Http;

internal class NSwaggerProvider : INSwaggerProvider
{
    private readonly PathProcessor _pathProcessor;
    private readonly SwaggerKeyRoles _keyRoles;
    private readonly SchemaGeneratorOptions _swaggerOptions;
    private readonly DocXmlOptions _docXmlOptions;
    private readonly OpenApiDocument _doc;

    public NSwaggerProvider(PathProcessor pathProcessor, SwaggerKeyRoles keyRoles,
        IOptions<SchemaGeneratorOptions> swaggerOptions, IOptions<DocXmlOptions> docXmlOptions)
    {
        _pathProcessor = pathProcessor;
        _keyRoles = keyRoles;
        _swaggerOptions = swaggerOptions.Value;
        _docXmlOptions = docXmlOptions.Value;
        _doc = new OpenApiDocument();
    }

    public OpenApiDocument GetSwagger(string? apiRootPath, List<ContractInfo> contracts, string? key)
    {
        Process(apiRootPath, contracts, key);
        return _doc;
    }

    private void Process(string? apiRootPath, List<ContractInfo> contracts, string? key)
    {
        //reset xml for inner type
        XmlHelper.ResetXmlForInnerType(_swaggerOptions, _docXmlOptions, contracts);

        //tags
        ProcessTags(contracts);

        //path
        ProcessPath(apiRootPath, contracts, key);

        //Components
        ProcessComponents(contracts);

        //clearTags
        ClearTags();
    }

    private void ProcessTags(List<ContractInfo> contracts)
    {
        var tags = new List<string>();
        contracts.ForEach(i => tags.AddRange(i.Tags));
        var distTags = tags.Distinct();
        foreach (var distTag in distTags)
            _doc.Tags.Add(new OpenApiTag { Name = distTag });
    }

    private void ProcessComponents(List<ContractInfo> contracts)
    {
        // 不加info会有一个验证错
        // _doc.Info = new OpenApiInfo
        // {
        //     Title = "",
        //     Version = ""
        // };

        //Schemas
        _doc.Components = new OpenApiComponents
        {
            Schemas = _pathProcessor.SchemaRepository.Schemas
        };

        //SecurityScheme
        var dic = new Dictionary<string, SecurityApiKeyDefineAttribute>();
        contracts.ForEach(i => i.SecurityApiKeyDefineAttributes.ToList().ForEach(j => dic[j.Key] = j));
        foreach (var item in dic.Values)
        {
            var scheme = new OpenApiSecurityScheme
            {
                Description = item.Description,
                Name = item.Name,
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                UnresolvedReference = false
            };
            _doc.Components.SecuritySchemes[item.Key] = scheme;
        }
    }

    private void ProcessPath(string? apiRootPath, IEnumerable<ContractInfo> contracts, string? key)
    {
        _doc.Paths = new OpenApiPaths();
        foreach (var contract in contracts)
        {
            var roles = _keyRoles.GetRoles(key);
            var roleMethods = contract.GetMethods(roles);
            foreach (var contractMethod in roleMethods)
            {
                foreach (var route in contractMethod.Route.SwaggerRouts)
                {
                    var pathItem = new OpenApiPathItem();
                    foreach (var method in route.HttpMethods)
                    {
                        //AddOperation 
                        var operation = _pathProcessor.Process(contractMethod, route, method);
                        pathItem.AddOperation(method.ToOperationType(), operation);
                    }

                    //add a path
                    //_doc.Paths.Add($"{apiRootPath}/{route.Path}", pathItem);
                    AddPath($"{apiRootPath}/{route.Path}", pathItem);
                }
            }
        }
    }

    private void ClearTags()
    {
        List<OpenApiTag> toRemove = new();
        List<OpenApiTag> existed = new();

        foreach (var i in _doc.Paths.Values)
        {
            foreach (var j in i.Operations.Values)
                existed.AddRange(j.Tags);
        }

        foreach (var tag in _doc.Tags)
        {
            if (!existed.Exists(i => i.Name == tag.Name))
                toRemove.Add(tag);
        }

        toRemove.ForEach(i => _doc.Tags.Remove(i));
    }

    private void AddPath(string key, OpenApiPathItem pathItem)
    {
        if (_doc.Paths.TryGetValue(key, out var v))
        {
            foreach (var (operationType, operation) in pathItem.Operations)
                v.AddOperation(operationType, operation);
        }
        else
            _doc.Paths.Add(key, pathItem);
    }
}