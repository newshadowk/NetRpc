using System;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace NetRpc.Http
{
    public static class OpenApiAnyFactoryOld
    {
        public static IOpenApiAny? CreateFor(OpenApiSchema schema, object? value)
        {
            if (value == null)
                return null;

            if (schema.Type == "integer" && schema.Format == "int64" && TryCast(value, out long typedValue1))
                return new OpenApiLong(typedValue1);
            if (schema.Type == "integer" && TryCast(value, out int typedValue2))
                return new OpenApiInteger(typedValue2);
            if (schema.Type == "number" && schema.Format == "double" && TryCast(value, out double typedValue3))
                return new OpenApiDouble(typedValue3);
            if (schema.Type == "number" && TryCast(value, out float typedValue4))
                return new OpenApiFloat(typedValue4);
            if (schema.Type == "boolean" && TryCast(value, out bool typedValue5))
                return new OpenApiBoolean(typedValue5);
            if (schema.Type == "string" && schema.Format == "date" && TryCast(value, out DateTime typedValue6))
                return new OpenApiDate(typedValue6);
            if (schema.Type == "string" && schema.Format == "date-time" && TryCast(value, out DateTime typedValue7))
                return new OpenApiDate(typedValue7);
            if (schema.Type == "string" && value.GetType().IsEnum)
                return new OpenApiString(Enum.GetName(value.GetType(), value));
            return schema.Type == "string" ? new OpenApiString(value.ToString()) : null;
        }

        private static bool TryCast<T>(object value, out T? typedValue)
        {
            try
            {
                typedValue = (T) Convert.ChangeType(value, typeof(T));
                return true;
            }
            catch
            {
                typedValue = default;
                return false;
            }
        }
    }
}