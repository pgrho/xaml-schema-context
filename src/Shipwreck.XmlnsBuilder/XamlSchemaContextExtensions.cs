using System.IO;
using System.Xaml;
using System.Xml;

namespace Shipwreck.XmlnsBuilder
{
    public static class XamlSchemaContextExtensions
    {
        public static string Serialize(this XamlSchemaContext schemaContext, object obj)
        {
            using (var xor = new XamlObjectReader(obj, schemaContext))
            using (var sw = new StringWriter())
            using (var xw = XmlWriter.Create(sw, new XmlWriterSettings
            {
                OmitXmlDeclaration = true
            }))
            using (var xxw = new XamlXmlWriter(xw, schemaContext))
            {
                while (xor.Read())
                {
                    xxw.WriteNode(xor);
                }
                xxw.Flush();
                xw.Flush();
                return sw.ToString();
            }
        }

        public static object Deserialize(this XamlSchemaContext schemaContext, string xaml)
        {
            using (var sr = new StringReader(xaml))
            using (var xor = new XamlXmlReader(sr, schemaContext))
            using (var xxw = new XamlObjectWriter(schemaContext))
            {
                while (xor.Read())
                {
                    xxw.WriteNode(xor);
                }

                return xxw.Result;
            }
        }

        public static T Deserialize<T>(this XamlSchemaContext schemaContext, string xaml)
            => (T)Deserialize(schemaContext, xaml);
    }
}
