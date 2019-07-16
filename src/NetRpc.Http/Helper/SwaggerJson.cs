using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using NJsonSchema;
using NJsonSchema.Generation;
using NSwag;

namespace NetRpc.Http
{
    public class SwaggerJson
    {
        private readonly string _apiRootPath;
        private readonly object[] _instances;
        private readonly OpenApiDocument _doc;

        public SwaggerJson(string apiRootPath, object[] instances)
        {
            _apiRootPath = apiRootPath;
            _instances = instances;
            _doc = new OpenApiDocument();
            _doc.SchemaType = SchemaType.Swagger2;

            //_doc.Consumes.Add("text/plain");
            //_doc.Consumes.Add("application/json");

            //_doc.Produces.Add("text/plain");
            //_doc.Produces.Add("application/json");
        }

        public string ToJson()
        {
            var doc = GetOpenApiDocument();
            return doc.ToJson();
        }

        public OpenApiDocument GetOpenApiDocument()
        {
            (List<Type> paramTypes, List<(Type interfaceInstance, List<MethodInfo> methods)> list) = GetMethodInfos(_instances);

            //model
            //merge all param to one json obj for each method.
            paramTypes.ForEach(i => _doc.Definitions.Add(i.Name, JsonSchema.FromType(i, new JsonSchemaGeneratorSettings
            {
                SchemaType = SchemaType.Swagger2
            })));

            //tag
            list.ForEach(i => _doc.Tags.Add(new OpenApiTag { Name = i.interfaceInstance.Name }));

            //path
            foreach (var tagGroup in list)
            {
                foreach (var method in tagGroup.methods)
                {
                    var key = $"{_apiRootPath}/{tagGroup.interfaceInstance.Name}/{method.Name}";
                    _doc.Paths[key] = new OpenApiPathItem();
                    var operation = new OpenApiOperation();
                    operation.Tags.Add(tagGroup.interfaceInstance.Name);
                    operation.Responses.Add("200", GetOpenApiResponse(method.ReturnType));
                    //var param = GetOpenApiParameter(method);
                    //operation.Parameters.Add(param);
                    foreach (var param in method.GetParameters())
                    {
                        var apiParam = GetOpenApiParameter(param.Name, param.ParameterType);
                        operation.Parameters.Add(apiParam);
                    }

                    _doc.Paths[key]["post"] = operation;
                }
            }

            return _doc;
        }

        private static (List<Type> paramTypes, List<(Type interfaceInstance, List<MethodInfo> methods)>) GetMethodInfos(object[] instances)
        {
            var list = new List<(Type interfaceInstance, List<MethodInfo> methods)>();
            var paramTypes = new List<Type>();

            foreach (var i in instances)
            {
                foreach (var m in GetMethodInfos(i))
                {
                    paramTypes.AddRange(m.paramTypes);
                    list.Add((m.interfaceInstance, m.methods));
                }
            }

            paramTypes = paramTypes.Distinct().ToList();
            return (paramTypes, list);
        }

        private static List<(Type interfaceInstance, List<Type> paramTypes, List<MethodInfo> methods)> GetMethodInfos(object instance)
        {
            var ret = new List<(Type interfaceInstance, List<Type> paramTypes, List<MethodInfo> methods)>();
            var instanceType = instance.GetType();
            foreach (var item in instanceType.GetInterfaces())
                ret.Add(GetMethodInfosByInterface(item));

            return ret;
        }

        private static (Type interfaceInstance, List<Type> paramTypes, List<MethodInfo> methods) GetMethodInfosByInterface(Type interfaceInstance)
        {
            var ms = interfaceInstance.GetMethods();
            var types = new List<Type>();
            ms.ToList().ForEach(i => types.AddRange(GetTypes(i)));
            return (interfaceInstance, types, ms.ToList());
        }

        private static List<Type> GetTypes(MethodInfo methodInfo)
        {
            var ret = new List<Type>();
            foreach (var p in methodInfo.GetParameters())
            {
                var t = GetTypeByParamTypeRecursive(p.ParameterType);
                ret.AddRange(t);
            }

            return ret;
        }

        private static List<Type> GetTypeByParamTypeRecursive(Type paramType)
        {
            List<Type> ret = new List<Type>();
            var t = GetTypeByParamType(paramType);
            if (t != null)
                ret.Add(t);

            foreach (var p in paramType.GetProperties())
            {
                t = GetTypeByParamType(p.PropertyType);

                if (t == null)
                    continue;

                if (ret.Exists(i => i.FullName == t.FullName))
                    continue;

                var subList = GetTypeByParamTypeRecursive(t);
                ret.Add(t);
                ret.AddRange(subList);
            }

            return ret;
        }

        private static Type GetTypeByParamType(Type paramType)
        {
            if (paramType == typeof(CancellationToken?) || paramType == typeof(CancellationToken))
                return null;

            Type t;
            if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Action<>))
                t = paramType.GenericTypeArguments[0];
            else
                t = paramType;

            if (NetRpc.Helper.IsSystemType(t))
                return null;

            return t;
        }

        private static OpenApiResponse GetOpenApiResponse(Type type)
        {
            var retType = Helper.GetTypeFromReturnTypeDefinition(type);
            var (rType, _, format) = GetJsonObjectTypeForParam(retType);
            var ret = new OpenApiResponse();

            var hasStream = retType.HasStream();
            if (hasStream)
                ret.ActualResponse.Content.Add("application/octet-stream", new OpenApiMediaType { Schema = new JsonSchema { Type = rType, Format = format } });
            else
                ret.ActualResponse.Content.Add("application/json", new OpenApiMediaType { Schema = new JsonSchema { Type = rType, Format = format } });
            return ret;
        }

        private static (JsonObjectType type, OpenApiParameterKind kind, string format) GetJsonObjectTypeForParam(Type type)
        {
            string format = null;
            var rType = JsonObjectType.String;
            var kind = OpenApiParameterKind.Body;

            if (type == typeof(Guid))
            {
                format = JsonFormatStrings.Guid;
                rType = JsonObjectType.String;
            }
            else if (type == typeof(byte) ||
                     type == typeof(int) ||
                     type == typeof(short) ||
                     type == typeof(long) ||
                     type == typeof(uint) ||
                     type == typeof(ushort) ||
                     type == typeof(ulong))
            {
                format = JsonFormatStrings.Integer;
                rType = JsonObjectType.Integer;
            }
            else if (type == typeof(float) ||
                     type == typeof(double) ||
                     type == typeof(decimal))
            {
                format = JsonFormatStrings.Double;
                rType = JsonObjectType.Number;
            }
            else if (type == typeof(bool))
            {
                rType = JsonObjectType.Boolean;
            }
            else if (type == typeof(string) ||
                     type == typeof(char))
            {
                rType = JsonObjectType.String;
            }
            else if (type == typeof(Stream))
            {
                rType = JsonObjectType.String;
                format = JsonFormatStrings.Binary;
                kind = OpenApiParameterKind.FormData;
            }

            return (rType, kind, format);
        }

        private OpenApiParameter GetOpenApiParameter(MethodInfo method)
        {
            var argType = Helper.GetArgType(method);
            var p = new OpenApiParameter();
            p.Name = "inParam";
            p.Kind = OpenApiParameterKind.Body;
            p.Schema = JsonSchema.FromType(argType, new JsonSchemaGeneratorSettings(){SchemaType = SchemaType.Swagger2});
            return p;
        }

        private OpenApiParameter GetOpenApiParameter(string name, Type type)
        {
            var (rType, kind, format) = GetJsonObjectTypeForParam(type);

            var p = new OpenApiParameter();
            p.Name = name;
            p.Kind = kind;
            p.IsRequired = true;
            p.Schema = new OpenApiParameter();

            //Schema
            if (_doc.Definitions.TryGetValue(type.Name, out var def))
                p.Schema.Reference = def;
            else
            {
                p.Schema.Type = rType;
                p.Schema.Format = format;
            }

            if (type == typeof(Stream))
                p.Type = JsonObjectType.File;
            return p;
        }
    }
}