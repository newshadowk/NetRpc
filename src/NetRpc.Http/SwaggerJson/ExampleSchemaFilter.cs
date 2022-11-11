using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using NetRpc.Contract;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NetRpc.Http;

public class ExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null || schema.Properties.Count == 0)
            return;

        //if (context.ApiModel == null || context.ApiModel.Type == null)
        //    return;

        var propertyInfos = context.Type.GetProperties();
        foreach (var propertyInfo in propertyInfos)
        {
            var example = propertyInfo.GetCustomAttribute<ExampleAttribute>();
            var name = GetName(propertyInfo);

            if (example != null)
            {
                foreach (KeyValuePair<string, OpenApiSchema> property in schema.Properties)
                {
                    if (name == property.Key)
                    {
                        if (example.Value == null)
                        {
                            property.Value.Example = new OpenApiNull();
                            break;
                        }

                        property.Value.Example = OpenApiAnyFactoryOld.CreateFor(property.Value, example.Value);
                        //property.Value.Example = OpenApiAnyFactory.TryCreateFor(property.Value, attribute.Value, out IOpenApiAny openApiAny)
                        //    ? openApiAny
                        //    : null;
                    }
                }
            }
        }
    }

    private static string GetName(PropertyInfo p)
    {
        var jsonName = p.GetCustomAttribute<JsonPropertyNameAttribute>();
        var name = jsonName == null ? p.Name : jsonName.Name;
        return ToCamelCase(name);
    }

    private static string ToCamelCase(string name)
    {
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}