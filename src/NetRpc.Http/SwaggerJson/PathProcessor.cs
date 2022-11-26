using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NetRpc.Contract;
using NetRpc.Http.Client;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NetRpc.Http;

internal class PathProcessor
{
    private readonly ISchemaGenerator _schemaGenerator;
    public readonly SchemaRepository SchemaRepository = new();
    private readonly SwaggerGeneratorOptions _options;

    public PathProcessor(ISchemaGenerator schemaGenerator, IOptions<SwaggerGeneratorOptions> options)
    {
        _schemaGenerator = schemaGenerator;
        _options = options.Value;
    }

    public OpenApiOperation? Process(ContractMethod contractMethod, HttpRoutInfo routInfo, HttpMethodAttribute method)
    {
        if (contractMethod.IsHttpIgnore)
            return null;

        //is Support?
        var isSupportBody = IsSupportBody(method.ToOperationType());

        //Operation
        var operation = new OpenApiOperation
        {
            Tags = GenerateTags(contractMethod),
            Responses = GenerateResponses(contractMethod, routInfo.MergeArgType.CancelToken != null),
            Parameters = new List<OpenApiParameter>()
        };

        //Obsolete
        if (method.Obsolete)
            operation.Deprecated = true;

        //Header
        AddHeader(contractMethod, operation);

        //Params to body or Parameters
        if (isSupportBody)
        {
            AddPathParams(contractMethod, operation, routInfo);
            if (routInfo.MergeArgType.TypeWithoutPathQueryStream != null || routInfo.MergeArgType.StreamPropName != null)
                operation.RequestBody = GenerateRequestBody(routInfo.MergeArgType.TypeWithoutPathQueryStream, routInfo.MergeArgType.StreamPropName);
        }
        else
            AddQueryPathParams(contractMethod, operation, routInfo);

        //Summary
        AddSummary(contractMethod, operation, routInfo);

        //ApiSecurity
        AddApiSecurity(contractMethod, operation);

        return operation;
    }

    private void AddPathParams(ContractMethod contractMethod, OpenApiOperation operation, HttpRoutInfo routInfo)
    {
        ValidatePath(contractMethod, routInfo);

        foreach (var p in contractMethod.InnerSystemTypeParameters)
        {
            if (routInfo.IsPath(p.DefineName))
            {
                var schema = _schemaGenerator.GenerateSchema(p.Type, SchemaRepository, p.PropertyInfo!, p.ParameterInfo!);
                operation.Parameters.Add(new OpenApiParameter
                {
                    In = ParameterLocation.Path,
                    Name = p.DefineName,
                    Schema = schema,
                    Description = schema.Description,
                    Required = true
                });
            }
        }
    }

    private void AddQueryPathParams(ContractMethod contractMethod, OpenApiOperation operation, HttpRoutInfo routInfo)
    {
        ValidatePath(contractMethod, routInfo);

        foreach (var p in contractMethod.InnerSystemTypeParameters)
        {
            var schema = _schemaGenerator.GenerateSchema(p.Type, SchemaRepository, p.PropertyInfo, p.ParameterInfo);
            bool required = true;
            if (p.AllowNullValue)
            {
                schema.Nullable = true;
                required = false;
            }

            if (routInfo.IsPath(p.DefineName))
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    In = ParameterLocation.Path,
                    Name = p.DefineName,
                    Schema = schema,
                    Description = schema.Description,
                    Required = required
                });
            }
            else
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    In = ParameterLocation.Query,
                    Name = p.DefineName,
                    Schema = schema,
                    Description = schema.Description,
                    Required = required
                });
            }
        }
    }

    private static void ValidatePath(ContractMethod contractMethod, HttpRoutInfo rout)
    {
        //validate
        foreach (var p in rout.PathParams)
        {
            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            if (contractMethod.InnerSystemTypeParameters.All(i => i.DefineName.ToLower() != p))
                throw new InvalidOperationException(
                    $"{rout.Path}, '{p}' is not found in method params:{contractMethod.InnerSystemTypeParameters.Select(i => i.DefineName).ListToString(", ")}");
        }
    }

    private static void AddHeader(ContractMethod contractMethod, OpenApiOperation operation)
    {
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
    }

    private void AddSummary(ContractMethod contractMethod, OpenApiOperation operation, HttpRoutInfo routInfo)
    {
        var filterContext = new OperationFilterContext(new ApiDescription(), _schemaGenerator, SchemaRepository, contractMethod.MethodInfo);
        foreach (var filter in _options.OperationFilters)
            filter.Apply(operation, filterContext);
        operation.Summary = AppendSummaryByCallbackAndCancel(operation.Summary, routInfo.MergeArgType.CallbackAction,
            routInfo.MergeArgType.CancelToken);
    }

    private static void AddApiSecurity(ContractMethod contractMethod, OpenApiOperation operation)
    {
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
    }

    private static List<OpenApiTag> GenerateTags(ContractMethod method)
    {
        var tags = new List<OpenApiTag>();
        method.Tags.ToList().ForEach(i => tags.Add(new OpenApiTag { Name = i }));
        return tags;
    }

    private OpenApiRequestBody GenerateRequestBody(Type? argType, string? streamName)
    {
        var body = new OpenApiRequestBody();

        if (argType != null)
        {
            body.Required = true;
            if (streamName != null)
            {
                GenerateRequestBodyByForm(body,
                    new TypeName("data", argType),
                    new TypeName(streamName, typeof(IFormFile))
                );
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

        if (!method.IsHideFaultExceptionDescription)
        {
            //600 cancel
            if (hasCancel)
                ret.Add(ClientConst.CancelStatusCode.ToString(), new OpenApiResponse { Description = "A task was canceled." });

            //exception
            GenerateException(ret, method);
        }

        return ret;
    }

    private static string AppendSummaryByCallbackAndCancel(string? oldDes, TypeName? action, TypeName? cancelToken)
    {
        oldDes ??= "";

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
        var properties = new Dictionary<string, OpenApiSchema?>();
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
                _ => new OpenApiEncoding { Style = ParameterStyle.Form }
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

    private OpenApiSchema? GenerateSchema(Type type)
    {
        if (type == typeof(Task))
            return null;

        if (type.Name.StartsWith("SimObj"))
        {
        }

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
}