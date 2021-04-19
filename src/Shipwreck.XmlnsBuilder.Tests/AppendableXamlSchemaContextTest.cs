using System;
using System.Globalization;
using System.IO;
using System.Xaml;
using System.Xml.Linq;
using Xunit;

namespace Shipwreck.XmlnsBuilder
{
    public class AppendableXamlSchemaContextTest
    {
        private static AppendableXamlSchemaContext GetContext()
            => new AppendableXamlSchemaContext()
                .AddNamespace("test://sys", typeof(int).Namespace, typeof(int).Assembly.GetName().Name);

        [Theory]
        [InlineData("<Int32 xmlns='clr-namespace:System;assembly=mscorlib'>0</Int32>", typeof(int), "0")]
        [InlineData("<Byte xmlns='test://sys'>10</Byte>", typeof(byte), "10")]
        public void DeserializationTest(string xaml, Type expectedType, string expectedValue)
        {
            var sc = GetContext();
            using (var sr = new StringReader(xaml))
            using (var xxr = new XamlXmlReader(sr, sc))
            using (var xow = new XamlObjectWriter(sc))
            {
                while (xxr.Read())
                {
                    xow.WriteNode(xxr);
                }

                Assert.IsType(expectedType, xow.Result);
                Assert.Equal(
                    expectedValue,
                    (xow.Result as IFormattable)?.ToString(null, CultureInfo.InvariantCulture) ?? xow.Result?.ToString());
            }
        }

        [Theory]
        [InlineData(typeof(AppendableXamlSchemaContextTest), "clr-namespace:Shipwreck.XmlnsBuilder;assembly=Shipwreck.XmlnsBuilder.Tests", nameof(AppendableXamlSchemaContextTest))]
        [InlineData(typeof(int), "test://sys", nameof(Int32))]
        public void SerializeTest(Type valueType, string xmlns, string name)
        {
            var v = Activator.CreateInstance(valueType);

            var sc = GetContext();
            using (var xor = new XamlObjectReader(v, sc))
            using (var sw = new StringWriter())
            using (var xxw = new XamlXmlWriter(sw, sc))
            {
                while (xor.Read())
                {
                    xxw.WriteNode(xor);
                }

                xxw.Flush();

                var xaml = sw.ToString();

                var xd = XDocument.Parse(xaml);

                Assert.Equal(XName.Get(name, xmlns), xd.Root.Name);
            }
        }
    }
}
