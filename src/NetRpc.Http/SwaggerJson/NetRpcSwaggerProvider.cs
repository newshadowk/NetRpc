using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NetRpc.Http.FaultContract;
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

        private void Process(string apiRootPath, object[] instances)
        {
            var list = GetMethodInfos(instances);

            //tag
            list.ForEach(i => _doc.Tags.Add(new OpenApiTag { Name = i.interfaceInstance.Name }));

            //path
            _doc.Paths = new OpenApiPaths();
            foreach (var tagGroup in list)
            {
                foreach (var method in tagGroup.methods)
                {
                    //Operation
                    var operation = new OpenApiOperation
                    {
                        Tags = GenerateTags(tagGroup.interfaceInstance.Name),
                        RequestBody = GenerateRequestBody(method, out var action, out var cancelToken),
                        Responses = GenerateResponses(method),
                    };

                    //Summary
                    var filterContext = new OperationFilterContext(new ApiDescription(), _schemaGenerator, _schemaRepository, method);
                    foreach (var filter in _options.OperationFilters)
                        filter.Apply(operation, filterContext);
                    operation.Summary = AppendSummaryByCallbackAndCancel(operation.Summary, action, cancelToken);

                    //Path
                    var openApiPathItem = new OpenApiPathItem();
                    openApiPathItem.AddOperation(OperationType.Post, operation);
                    var key = $"{apiRootPath}/{tagGroup.interfaceInstance.Name}/{method.Name}";
                    _doc.Paths.Add(key, openApiPathItem);
                }
            }

            _doc.Components = new OpenApiComponents
            {
                Schemas = _schemaRepository.Schemas
            };
        }
   
        private static List<(Type interfaceInstance, List<MethodInfo> methods)> GetMethodInfos(object[] instances)
        {
            var list = new List<(Type interfaceInstance, List<MethodInfo> methods)>();

            foreach (var i in instances)
            {
                foreach (var m in GetMethodInfos(i))
                {
                    list.Add((m.interfaceInstance, m.methods));
                }
            }

            return list;
        }

        private static List<(Type interfaceInstance, List<MethodInfo> methods)> GetMethodInfos(object instance)
        {
            var ret = new List<(Type interfaceInstance, List<MethodInfo> methods)>();
            var instanceType = instance.GetType();
            foreach (var item in instanceType.GetInterfaces())
                ret.Add((item, item.GetMethods().ToList()));
            return ret;
        }

        private static List<OpenApiTag> GenerateTags(string tagName)
        {
            return new List<OpenApiTag>{ new OpenApiTag { Name = tagName }};
        }

        private OpenApiResponses GenerateResponses(MethodInfo method)
        {
            OpenApiResponses ret = new OpenApiResponses();

            var returnType = Helper.GetTypeFromReturnTypeDefinition(method.ReturnType);

            //200
            var res200 = new OpenApiResponse();
            var hasStream = returnType.HasStream();
            if (hasStream)
            {
                res200.Description = "file";
                res200.Content.Add("application/json", new OpenApiMediaType()
                {
                    Schema = GenerateSchema(typeof(IFormFile))
                });
            }
            else
            {
                res200.Content.Add("application/json", new OpenApiMediaType()
                {
                    Schema = GenerateSchema(returnType)
                });
            }

            ret.Add("200", res200);

            //400
            var faultContractTypes = GetFaultContractTypes(method);
            if (faultContractTypes.Count == 0)
                return ret;

            var res400 = new OpenApiResponse();
            res400.Content.Add("application/json", new OpenApiMediaType
            {
                Schema = GenerateSchema(typeof(Fault))
            });

            res400.Description += "<b>Name</b> : Name of the exception.<br/><b>Details</b> : Maybe one of model below:";
            foreach (var detailsType in faultContractTypes)
            {
                GenerateSchema(detailsType);
                res400.Description += $"<br/> <b>{detailsType.Name}</b>";
            }

            ret.Add("400", res400);
            return ret;
        }

        private static List<Type> GetFaultContractTypes(MethodInfo method)
        {
            var ret = new List<Type>();
            foreach (SwaggerFaultContractAttribute a in method.GetCustomAttributes(typeof(SwaggerFaultContractAttribute), true))
                ret.Add(a.DetailType);
            return ret;
        }

        private OpenApiRequestBody GenerateRequestBody(MethodInfo method, out TypeName action, out TypeName cancelToken)
        {
            var body = new OpenApiRequestBody();

            var argType = Helper.GetArgType(method, out var streamName, out var outAction, out var outCancelToken);
            action = outAction;
            cancelToken = outCancelToken;

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

            string append = "";
            if (action != null)
                append += $"[Callback]{action.Type.GetGenericArguments()[0].Name} {action.Name}, ";

            if (cancelToken != null)
                append += $"[CancelToken]{cancelToken.Name}";

            append.TrimEndString("");

            if (append != "")
                return $"{oldDes}, {append}";

            return oldDes;
        }

        private void GenerateRequestBodyByForm(OpenApiRequestBody body, params TypeName[] typeNames)
        {
            var properties = new Dictionary<string, OpenApiSchema>();
            foreach (var typeName in typeNames)
                properties.Add(typeName.Name, GenerateSchema(typeName.Type));

            var openApiSchema = new OpenApiSchema
            {
                Type = "object",
                Properties = properties,
            };

            body.Content.Add("multipart/form-data", new OpenApiMediaType
            {
                Schema = openApiSchema,
                Encoding = openApiSchema.Properties.ToDictionary(
                    entry => entry.Key,
                    entry => new OpenApiEncoding { Style = ParameterStyle.Form }
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

        public OpenApiDocument GetSwagger(string apiRootPath, object[] instances)
        {
            Process(apiRootPath, instances);
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