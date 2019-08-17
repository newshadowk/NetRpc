using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
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

        private void Process(string apiRootPath, IEnumerable<Type> instanceTypes)
        {
            var list = GetInterfaceMethodInfos(instanceTypes);

            //tag
            list.ForEach(i => _doc.Tags.Add(new OpenApiTag {Name = i.interfaceInstance.Name}));

            //path
            _doc.Paths = new OpenApiPaths();
            foreach (var tagGroup in list)
            {
                foreach (var method in tagGroup.methods)
                {
                    var argType = Helper.GetArgType(method, out var streamName, out var action, out var cancelToken);

                    //Operation
                    var operation = new OpenApiOperation
                    {
                        Tags = GenerateTags(tagGroup.interfaceInstance.Name),
                        RequestBody = GenerateRequestBody(argType, streamName),
                        Responses = GenerateResponses(method, cancelToken != null)
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

        private static List<(Type interfaceInstance, List<MethodInfo> methods)> GetInterfaceMethodInfos(IEnumerable<Type> instanceTypes)
        {
            var list = new List<(Type interfaceInstance, List<MethodInfo> methods)>();
            foreach (var i in instanceTypes)
            foreach (var m in GetInterfaceMethodInfos(i))
                list.Add((m.interfaceInstance, m.methods));
            return list;
        }

        private static List<(Type interfaceInstance, List<MethodInfo> methods)> GetInterfaceMethodInfos(Type instanceType)
        {
            var ret = new List<(Type interfaceInstance, List<MethodInfo> methods)>();
            foreach (var item in instanceType.GetInterfaces())
                ret.Add((item, item.GetMethods().ToList()));
            return ret;
        }

        private static List<OpenApiTag> GenerateTags(string tagName)
        {
            return new List<OpenApiTag> {new OpenApiTag {Name = tagName}};
        }

        private OpenApiResponses GenerateResponses(MethodInfo method, bool hasCancel)
        {
            var ret = new OpenApiResponses();
            var returnType = Helper.GetTypeFromReturnTypeDefinition(method.ReturnType);

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
                ret.Add(ConstValue.CancelStatusCode.ToString(), new OpenApiResponse {Description = "A task was canceled."});

            //exception  
            var resTypes = method.GetProducesResponseTypes();
            if (resTypes.Count == 0)
                return ret;

            foreach (var fault in resTypes)
            {
                var res = new OpenApiResponse();
                res.Content.Add("application/json", new OpenApiMediaType
                {
                    Schema = GenerateSchema(fault.DetailType)
                });

                ret.Add(fault.StatusCode.ToString(), res);
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

        public OpenApiDocument GetSwagger(string apiRootPath, IEnumerable<Type> instanceTypes)
        {
            Process(apiRootPath, instanceTypes);
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