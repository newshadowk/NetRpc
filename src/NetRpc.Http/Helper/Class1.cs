using System;
using Namotion.Reflection;
using NJsonSchema;
using NJsonSchema.Generation;
using NSwag;
using NSwag.Generation;

namespace NetRpc.Http
{
    public static class M1
    {
        public static void D()
        {
            var doc = new OpenApiDocument();
            var schemaResolver = new OpenApiSchemaResolver(doc, new JsonSchemaGeneratorSettings());
            var gs = new OpenApiDocumentGeneratorSettings();
            var generator = new OpenApiDocumentGenerator(gs, schemaResolver);

            var contextualType = typeof(C1).ToContextualType();
            generator.CreatePrimitiveParameter("p1", "p1 dec", contextualType);

            var json = doc.ToJson();
        }
    }

    public class C1
    {
        public string P1 { get; set; }

        public C2 C2 { get; set; }
    }

    public class C2
    {
        public string P2 { get; set; }
    }
}