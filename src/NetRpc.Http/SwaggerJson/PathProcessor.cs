using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NetRpc.Http.Client;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NetRpc.Http
{
    internal sealed class PathItem
    {
        public string Key { get; }

        public OpenApiPathItem Item { get; }

        public PathItem(string key, OpenApiPathItem item)
        {
            Key = key;
            Item = item;
        }
    }

    internal class PathProcessor
    {
        private readonly ISchemaGenerator _schemaGenerator;
        public readonly SchemaRepository SchemaRepository = new SchemaRepository();
        private readonly SwaggerGeneratorOptions _options;

        public PathItem Process(string apiRootPath, Contract contract, ContractMethod contractMethod, OperationType operationType)
        {
            if (contractMethod.IsHttpIgnore)
                return null;

            //Operation
            var operation = new OpenApiOperation
            {
                Tags = GenerateTags(contract, contractMethod),
                Responses = GenerateResponses(contractMethod, contractMethod.MergeArgType.CancelToken != null)
            };

            //Header
            operation.Parameters = new List<OpenApiParameter>();
            foreach (var header in contractMethod.HttpHeaderAttributes)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    In = ParameterLocation.Header,
                    Name = header.Name,
                    Description = header.Description,
                    Schema = new OpenApiSchema { Type = "string" }
                });
            }

            //Params to body or Parameters
            if (IsSupportBody(operationType))
                operation.RequestBody = GenerateRequestBody(contractMethod.MergeArgType.TypeWithoutStreamName, contractMethod.MergeArgType.StreamPropName);
            else
            {
                
            }

            //Summary
            var filterContext = new OperationFilterContext(new ApiDescription(), _schemaGenerator, SchemaRepository, contractMethod.MethodInfo);
            foreach (var filter in _options.OperationFilters)
                filter.Apply(operation, filterContext);
            operation.Summary = AppendSummaryByCallbackAndCancel(operation.Summary, contractMethod.MergeArgType.CallbackAction,
                contractMethod.MergeArgType.CancelToken);


            //ApiSecurity
            foreach (var apiKey in contractMethod.SecurityApiKeyAttributes)
            {
                var r = new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Id = apiKey.Key,
                                Type = ReferenceType.SecurityScheme
                            },
                            UnresolvedReference = true
                        },
                        new List<string>()
                    }
                };
                operation.Security.Add(r);
            }

            var openApiPathItem = new OpenApiPathItem();
            openApiPathItem.AddOperation(operationType, operation);
            var key = $"{apiRootPath}/{contractMethod.HttpRoutInfo}";
            return new PathItem(key, openApiPathItem);
        }

        public PathProcessor(ISchemaGenerator schemaGenerator, IOptions<SwaggerGeneratorOptions> options)
        {
            _schemaGenerator = schemaGenerator;
            _options = options.Value;
        }

        private static List<OpenApiTag> GenerateTags(Contract contract, ContractMethod method)
        {
            var tags = new List<OpenApiTag>();
            tags.Add(new OpenApiTag { Name = contract.ContractInfo.Route });
            method.TagAttributes.ForEach(i => tags.Add(new OpenApiTag { Name = i.Name }));
            return tags;
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

        private OpenApiRequestBody GenerateRequestBody2(Type argType, string streamName)
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

        private OpenApiResponses GenerateResponses(ContractMethod method, bool hasCancel)
        {
            var ret = new OpenApiResponses();
            var returnType = method.MethodInfo.ReturnType.GetTypeFromReturnTypeDefinition();

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
                ret.Add(ClientConstValue.CancelStatusCode.ToString(), new OpenApiResponse { Description = "A task was canceled." });

            //exception
            GenerateException(ret, method);
            return ret;
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

        private OpenApiSchema GenerateSchema(Type type)
        {
            if (type == typeof(Task))
                return null;

            return _schemaGenerator.GenerateSchema(type, SchemaRepository);
        }

        private void GenerateException(OpenApiResponses ret, ContractMethod method)
        {
            //merge Faults
            var allFaults = method.FaultExceptionAttributes;
            if (!allFaults.Any())
                return;

            foreach (var grouping in allFaults.GroupBy(i => i.StatusCode))
            {
                var des = "";
                foreach (var item in grouping)
                    des += $"ErrCode:{item.ErrorCode}, <b>{item.DetailType.Name}</b>, {item.Description}<br/>";
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

        private static bool IsSupportBody(OperationType type)
        {
            return type switch
            {
                OperationType.Get => false,
                OperationType.Head => false,
                OperationType.Trace => false,
                _ => true
            };
        }

        private static bool IsSupportParams(ContractMethod contractMethod)
        {
            if (contractMethod.Parameters.Count == 0)
                return false;

            if (contractMethod.Parameters.Count == 1)
            {
                if (contractMethod.Parameters[0].Type.GetProperties().Any(i => !i.PropertyType.IsSystemType()))
                    return false;
            }

            if (contractMethod.Parameters.Exists(i => !i.Type.IsSystemType()))
                return false;
            return true;
        }
    }
}