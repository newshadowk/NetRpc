using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NetRpc.Http.Client;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NetRpc.Http
{
    internal class NetRpcSwaggerProvider : INetRpcSwaggerProvider
    {
        private readonly ISchemaGenerator _schemaGenerator;
        private readonly SwaggerGeneratorOptions _options;
        private readonly SchemaRepository _schemaRepository;
        private readonly OpenApiDocument _doc;
        private volatile bool _supportCallbackAndCancel;

        public NetRpcSwaggerProvider(ISchemaGenerator schemaGenerator, IOptionsMonitor<HttpServiceOptions> httpServiceOptions,
            IOptions<SwaggerGeneratorOptions> optionsAccessor)
        {
            _schemaRepository = new SchemaRepository();
            _schemaGenerator = schemaGenerator;
            _options = optionsAccessor.Value;
            _doc = new OpenApiDocument();
            _supportCallbackAndCancel = httpServiceOptions.CurrentValue.SupportCallbackAndCancel;
            httpServiceOptions.OnChange(i => _supportCallbackAndCancel = i.SupportCallbackAndCancel);
        }

        private void Process(string apiRootPath, bool supportCallbackAndCancel, List<Contract> contracts)
        {
            var list = GetContractMethodInfos(contracts);

            //tag
            list.ForEach(i => _doc.Tags.Add(new OpenApiTag {Name = i.contractType.Name}));

            //path
            _doc.Paths = new OpenApiPaths();
            foreach (var tagGroup in list)
            {
                foreach (var method in tagGroup.methods)
                {
                    var argType = ClientHelper.GetArgType(method, supportCallbackAndCancel, out var streamName, out var action, out var cancelToken);

                    //Operation
                    var operation = new OpenApiOperation
                    {
                        Tags = GenerateTags(tagGroup.contractType.Name),
                        RequestBody = GenerateRequestBody(argType, streamName),
                        Responses = GenerateResponses(method, tagGroup.contractFaults, cancelToken != null)
                    };

                    //Summary
                    var filterContext = new OperationFilterContext(new ApiDescription(), _schemaGenerator, _schemaRepository, method);
                    foreach (var filter in _options.OperationFilters)
                        filter.Apply(operation, filterContext);
                    if (supportCallbackAndCancel)
                        operation.Summary = AppendSummaryByCallbackAndCancel(operation.Summary, action, cancelToken);

                    //Path
                    var openApiPathItem = new OpenApiPathItem();
                    openApiPathItem.AddOperation(OperationType.Post, operation);
                    var key = $"{apiRootPath}/{ClientHelper.GetActionPath(tagGroup.contractType, method)}";
                    _doc.Paths.Add(key, openApiPathItem);
                }
            }

            _doc.Components = new OpenApiComponents
            {
                Schemas = _schemaRepository.Schemas
            };
        }

        private static List<(Type contractType, List<FaultExceptionAttribute> contractFaults, List<MethodInfo> methods)> GetContractMethodInfos(
            List<Contract> contracts)
        {
            var list = new List<(Type contractType, List<FaultExceptionAttribute> contractFaults, List<MethodInfo> methods)>();
            foreach (var contract in contracts)
                list.Add((contract.ContractType, contract.ContractType.GetCustomAttributes<FaultExceptionAttribute>(true).ToList(),
                    contract.ContractType.GetInterfaceMethods().ToList()));
            return list;
        }

        private static List<OpenApiTag> GenerateTags(string tagName)
        {
            return new List<OpenApiTag> {new OpenApiTag {Name = tagName}};
        }

        private OpenApiResponses GenerateResponses(MethodInfo method, List<FaultExceptionAttribute> contractFaults, bool hasCancel)
        {
            var ret = new OpenApiResponses();
            var returnType = method.ReturnType.GetTypeFromReturnTypeDefinition();

            //200
            var res200 = new OpenApiResponse();
            var hasStream = returnType.HasStream();
            if (hasStream)
            {
                res200.Description = "file";
                res200.Content.Add("application/json", new OpenApiMediaType
                {
                    Schema = GenerateSchema(typeof(IFormFile))
                });
            }
            else
            {
                res200.Content.Add("application/json", new OpenApiMediaType
                {
                    Schema = GenerateSchema(returnType)
                });
            }

            ret.Add("200", res200);

            //600
            if (hasCancel)
                ret.Add(ClientConstValue.CancelStatusCode.ToString(), new OpenApiResponse {Description = "A task was canceled."});

            //exception  
            var resTypes = method.GetCustomAttributes<FaultExceptionAttribute>(true).ToList();
            resTypes.AddRange(contractFaults);
            if (!resTypes.Any())
                return ret;

            foreach (var grouping in resTypes.GroupBy(i => i.StatusCode))
            {
                string des = "";
                foreach (var item in grouping)
                    des += $"<b>{item.DetailType.Name}</b> ErrorCode:{item.ErrorCode}, {item.Summary}<br/>";
                des = des.TrimEndString("<br/>");
                ret.Add(grouping.Key.ToString(), new OpenApiResponse { Description = des });
            }

            return ret;
        }

        private OpenApiRequestBody GenerateRequestBody(Type argType, string streamName)
        {
            var body = new OpenApiRequestBody();

            if (argType != null)
            {
                body.Required = true;
                if (streamName != null)
                {
                    GenerateRequestBodyByForm(body,
                        new TypeName(streamName, typeof(IFormFile)),
                        new TypeName("data", argType));
                }
                else
                    GenerateRequestBodyByBody(body, argType);
            }
            else if (streamName != null)
            {
                body.Required = true;
                GenerateRequestBodyByForm(body, new TypeName(streamName, typeof(IFormFile)));
            }

            return body;
        }

        private static string AppendSummaryByCallbackAndCancel(string oldDes, TypeName action, TypeName cancelToken)
        {
            if (oldDes == null)
                oldDes = "";

            var append = "";
            if (action != null)
                append += $"[Callback]{action.Type.GetGenericArguments()[0].Name} {action.Name}, ";

            if (cancelToken != null)
                append += $"[CancelToken]{cancelToken.Name}";

            append.TrimEndString("");

            if (append != "" && oldDes != "")
                return $"{oldDes}, {append}";

            if (append != "" && oldDes == "")
                return $"{append}";

            return oldDes;
        }

        private void GenerateRequestBodyByForm(OpenApiRequestBody body, params TypeName[] typeNames)
        {
            var properties = new Dictionary<string, OpenApiSchema>();
            try
            {
                foreach (var typeName in typeNames)
                    properties.Add(typeName.Name, GenerateSchema(typeName.Type));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            var openApiSchema = new OpenApiSchema
            {
                Type = "object",
                Properties = properties
            };

            body.Content.Add("multipart/form-data", new OpenApiMediaType
            {
                Schema = openApiSchema,
                Encoding = openApiSchema.Properties.ToDictionary(
                    entry => entry.Key,
                    entry => new OpenApiEncoding {Style = ParameterStyle.Form}
                )
            });
        }

        private void GenerateRequestBodyByBody(OpenApiRequestBody body, Type type)
        {
            body.Content.Add("application/json", new OpenApiMediaType
            {
                Schema = GenerateSchema(type)
            });
        }

        public OpenApiDocument GetSwagger(string apiRootPath, List<Contract> contracts)
        {
            Process(apiRootPath, _supportCallbackAndCancel, contracts);
            return _doc;
        }

        private OpenApiSchema GenerateSchema(Type type)
        {
            if (type == typeof(Task))
                return null;

            return _schemaGenerator.GenerateSchema(type, _schemaRepository);
        }
    }
}