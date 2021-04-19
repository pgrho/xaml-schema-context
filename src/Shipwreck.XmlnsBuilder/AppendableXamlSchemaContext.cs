using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xaml;
using System.Xaml.Schema;

namespace Shipwreck.XmlnsBuilder
{
    public class AppendableXamlSchemaContext : XamlSchemaContext
    {
        protected sealed class CustomXamlType : XamlType
        {
            public CustomXamlType(Type underlyingType, AppendableXamlSchemaContext schemaContext)
                : base(underlyingType, schemaContext)
            {
            }

            public CustomXamlType(string typeName, IList<XamlType> typeArguments, AppendableXamlSchemaContext schemaContext)
                : base(typeName, typeArguments, schemaContext)
            {
            }

            public CustomXamlType(Type underlyingType, AppendableXamlSchemaContext schemaContext, XamlTypeInvoker invoker)
                : base(underlyingType, schemaContext, invoker)
            {
            }

            public CustomXamlType(string unknownTypeNamespace, string unknownTypeName, IList<XamlType> typeArguments, AppendableXamlSchemaContext schemaContext)
                : base(unknownTypeNamespace, unknownTypeName, typeArguments, schemaContext)
            {
            }

            private string[] _CustomNamespaces;

            public override IList<string> GetXamlNamespaces()
            {
                if (_CustomNamespaces == null)
                {
                    var nss = ((AppendableXamlSchemaContext)SchemaContext)._Namespaces;
                    var asm = UnderlyingType.Assembly.GetName().Name;
                    var ns = UnderlyingType.Namespace;
                    _CustomNamespaces = nss.Where(e => e.asm == asm && e.ns == ns).Select(e => e.xmlns).ToArray();
                }

                if (_CustomNamespaces.Any())
                {
                    return _CustomNamespaces.Concat(base.GetXamlNamespaces()).ToList();
                }

                return base.GetXamlNamespaces();
            }
        }

        private List<(string xmlns, string asm, string ns)> _Namespaces;
        private Dictionary<string, string> _Prefixes;

        public AppendableXamlSchemaContext AddNamespaces(string xmlns, Assembly @assembly)
            => AddNamespaces(xmlns, @assembly.GetExportedTypes().GroupBy(e => e.Namespace).Select(e => e.First()));

        public AppendableXamlSchemaContext AddNamespaces(string xmlns, IEnumerable<Type> namespaceTypes)
        {
            foreach (var t in namespaceTypes)
            {
                AddNamespace(xmlns, t);
            }
            return this;
        }

        public AppendableXamlSchemaContext AddNamespace(string xmlns, Type namespaceType)
            => AddNamespace(xmlns, namespaceType.Namespace, namespaceType.Assembly.GetName().Name);

        public AppendableXamlSchemaContext AddNamespace(string xmlns, string clrNamespace, string assemblyName)
        {
            (_Namespaces ??= new List<(string xmlns, string asm, string ns)>()).Add((xmlns, assemblyName, clrNamespace));
            return this;
        }

        public AppendableXamlSchemaContext AddPrefix(string xmlns, string prefix)
        {
            (_Prefixes ??= new Dictionary<string, string>())[xmlns] = prefix;
            return this;
        }

        public override IEnumerable<string> GetAllXamlNamespaces()
            => _Namespaces?.Count > 0 ? base.GetAllXamlNamespaces().Concat(_Namespaces.Select(e => e.xmlns)).Distinct() : base.GetAllXamlNamespaces();

        public override ICollection<XamlType> GetAllXamlTypes(string xamlNamespace)
            => base.GetAllXamlTypes(xamlNamespace);

        public override string GetPreferredPrefix(string xmlns)
        {
            if (_Prefixes != null && _Prefixes.TryGetValue(xmlns, out var r))
            {
                return r;
            }
            return base.GetPreferredPrefix(xmlns);
        }

        protected override XamlType GetXamlType(string xamlNamespace, string name, params XamlType[] typeArguments)
        {
            if (_Namespaces != null)
            {
                foreach (var tp in _Namespaces)
                {
                    if (tp.xmlns == xamlNamespace)
                    {
                        var xt = base.GetXamlType($"clr-namespace:{tp.ns};assembly={tp.asm}", name, typeArguments)
                                ?? base.GetXamlType($"clr-namespace:{tp.ns}", name, typeArguments);

                        if (xt != null)
                        {
                            return xt;
                        }
                    }
                }
            }
            return base.GetXamlType(xamlNamespace, name, typeArguments);
        }

        private readonly ConcurrentDictionary<Type, CustomXamlType> _Types = new ConcurrentDictionary<Type, CustomXamlType>();

        public override XamlType GetXamlType(Type type)
        {
            if (!_Types.TryGetValue(type, out var r))
            {
                r = new CustomXamlType(type, this);
                _Types.TryAdd(type, r);
            }
            return r;
        }

        public override bool TryGetCompatibleXamlNamespace(string xamlNamespace, out string compatibleNamespace)
        {
            if (_Namespaces != null && _Namespaces.Any(e => e.xmlns == xamlNamespace))
            {
                compatibleNamespace = xamlNamespace;
                return true;
            }
            return base.TryGetCompatibleXamlNamespace(xamlNamespace, out compatibleNamespace);
        }
    }
}
