using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NetRpc.Http
{
    public class ExampleSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties == null || schema.Properties.Count == 0)
                return;

            //if (context.ApiModel == null || context.ApiModel.Type == null)
            //    return;

            var propertyInfos = context.Type.GetProperties();
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                var attribute = propertyInfo.GetCustomAttribute<ExampleAttribute>();
                if (attribute != null)
                {
                    foreach (KeyValuePair<string, OpenApiSchema> property in schema.Properties)
                    {
                        if (ToCamelCase(propertyInfo.Name) == property.Key)
                        {
                            if (attribute.Value == null)
                            {
                                property.Value.Example = new OpenApiNull();
                                break;
                            }

                            property.Value.Example = OpenApiAnyFactory.CreateFor(property.Value, attribute.Value);

                            //property.Value.Example = OpenApiAnyFactory.TryCreateFor(property.Value, attribute.Value, out IOpenApiAny openApiAny)
                            //    ? openApiAny
                            //    : null;
                        }
                    }
                }
            }
        }

        private static string ToCamelCase(string name)
        {
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }
}
