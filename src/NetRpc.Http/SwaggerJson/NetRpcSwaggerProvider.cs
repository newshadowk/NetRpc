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

        public NetRpcSwaggerProvider(ISchemaGenerator schemaGenerator, IOptions<SwaggerGeneratorOptions> optionsAccessor)
        {
            _schemaRepository = new SchemaRepository();
            _schemaGenerator = schemaGenerator;
            _options = optionsAccessor.Value;
            _doc = new OpenApiDocument();
        }

        private void Process(string apiRootPath, List<Contract> contracts)
        {
            //tag
            contracts.ForEach(i => _doc.Tags.Add(new OpenApiTag {Name = i.Route}));

            //path
            _doc.Paths = new OpenApiPaths();
            foreach (var contract in contracts)
            {
                foreach (var methodObj in contract.MethodObjs)
                {
                    //Operation
                    var operation = new OpenApiOperation
                    {
                        Tags = GenerateTags(contract.ContractType.Name),
                        RequestBody = GenerateRequestBody(methodObj.MergeArgType.Type, methodObj.MergeArgType.StreamName),
                        Responses = GenerateResponses(methodObj.MethodInfo, contract, methodObj.MergeArgType.CancelToken != null)
                    };

                    //Summary
                    var filterContext = new OperationFilterContext(new ApiDescription(), _schemaGenerator, _schemaRepository, methodObj.MethodInfo);
                    foreach (var filter in _options.OperationFilters)
                        filter.Apply(operation, filterContext);
                    operation.Summary = AppendSummaryByCallbackAndCancel(operation.Summary, methodObj.MergeArgType.CallbackAction, methodObj.MergeArgType.CancelToken);

                    //Path
                    var openApiPathItem = new OpenApiPathItem();
                    openApiPathItem.AddOperation(OperationType.Post, operation);
                    var key = $"{apiRootPath}/{ClientHelper.GetActionPath(contract.ContractType, methodObj.MethodInfo)}";
                    _doc.Paths.Add(key, openApiPathItem);
                }
            }

            _doc.Components = new OpenApiComponents
            {
                Schemas = _schemaRepository.Schemas
            };
        }

        private static List<OpenApiTag> GenerateTags(string tagName)
        {
            return new List<OpenApiTag> {new OpenApiTag {Name = tagName}};
        }

        private OpenApiResponses GenerateResponses(MethodInfo method, Contract contract, bool hasCancel)
        {
            var ret = new OpenApiResponses();
            var returnType = method.ReturnType.GetTypeFromReturnTypeDefinition();

            //200 Ok
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

            //600 cancel
            if (hasCancel)
                ret.Add(ClientConstValue.CancelStatusCode.ToString(), new OpenApiResponse {Description = "A task was canceled."});

            //exception
            GenerateException(ret, method, contract);
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
            Process(apiRootPath, contracts);
            return _doc;
        }

        private OpenApiSchema GenerateSchema(Type type)
        {
            if (type == typeof(Task))
                return null;

            return _schemaGenerator.GenerateSchema(type, _schemaRepository);
        }

        private void GenerateException(OpenApiResponses ret, MethodInfo method, Contract contract)
        {
            //merge Faults
            var allFaults = contract.GetFaults(method);
            if (!allFaults.Any())
                return;

            foreach (var grouping in allFaults.GroupBy(i => i.StatusCode))
            {
                string des = "";
                foreach (var item in grouping)
                    des += $"<b>{item.DetailType.Name}</b> ErrorCode:{item.ErrorCode}, {item.Summary}<br/>";
                des = des.TrimEndString("<br/>");
                var resFault = new OpenApiResponse();
                resFault.Description = des;
                resFault.Content.Add("application/json", new OpenApiMediaType
                {
                    Schema = GenerateSchema(typeof(FaultExceptionJsonObj))
                });

                ret.Add(grouping.Key.ToString(), resFault);
            }
        }
    }
}