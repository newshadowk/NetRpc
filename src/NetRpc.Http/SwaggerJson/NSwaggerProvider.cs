using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace NetRpc.Http
{
    internal class NSwaggerProvider : INSwaggerProvider
    {
        private readonly PathProcessor _pathProcessor;
        private readonly OpenApiDocument _doc;

        public NSwaggerProvider(PathProcessor pathProcessor)
        {
            _pathProcessor = pathProcessor;
            _doc = new OpenApiDocument();
        }

        public OpenApiDocument GetSwagger(string apiRootPath, List<Contract> contracts)
        {
            Process(apiRootPath, contracts);
            return _doc;
        }

        private void Process(string apiRootPath, List<Contract> contracts)
        {
            //tags
            ProcessTags(contracts);

            //path
            ProcessPath(apiRootPath, contracts);

            //Components
            ProcessComponents(contracts);
        }

        private void ProcessTags(List<Contract> contracts)
        {
            var tags = new List<string>();
            contracts.ForEach(i => tags.AddRange(i.ContractInfo.Tags));
            var distTags = tags.Distinct();
            foreach (var distTag in distTags)
                _doc.Tags.Add(new OpenApiTag { Name = distTag });
        }

        private void ProcessComponents(List<Contract> contracts)
        {
            //Schemas
            _doc.Components = new OpenApiComponents
            {
                Schemas = _pathProcessor.SchemaRepository.Schemas
            };

            //SecurityScheme
            var dic = new Dictionary<string, SecurityApiKeyDefineAttribute>();
            contracts.ForEach(i => i.ContractInfo.SecurityApiKeyDefineAttributes.ToList().ForEach(j => dic[j.Key] = j));
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

        private void ProcessPath(string apiRootPath, List<Contract> contracts)
        {
            _doc.Paths = new OpenApiPaths();
            foreach (var contract in contracts)
            {
                foreach (var contractMethod in contract.ContractInfo.Methods)
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
                        _doc.Paths.Add($"{apiRootPath}/{route.Path}", pathItem);
                    }
                }
            }
        }
    }
}