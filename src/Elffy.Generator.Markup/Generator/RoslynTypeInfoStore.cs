#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Linq;
using U8Xml;
using Elffy.Markup;
using TypeInfo = Elffy.Markup.TypeInfo;

namespace Elffy.Generator;

public sealed class RoslynTypeInfoStore //: ITypeInfoStore
{
    private readonly Dictionary<string, TypeMemberDic> _typeDic;

    private RoslynTypeInfoStore(Dictionary<string, TypeMemberDic> typeDic)
    {
        _typeDic = typeDic;
    }

    public static RoslynTypeInfoStore Create(XmlObject xml, Compilation compilation, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var typeDic = new Dictionary<string, TypeMemberDic>();
        CollectXmlNodeInstanceTypes(xml, (typeDic, compilation), ct, static (x, typeFullName, ct) =>
        {
            var (typeDic, compilation) = x;

            // TODO: generic type
            // The arg of the method should be like "System.Collections.Generic.Dictionary`2".
            // (Can not input "System.Collections.Generic.Dictionary<string, int>")
            var type = compilation.GetTypeByMetadataName(typeFullName.ToString());

            if(type == null) { return; }
            var typeName = type.ToString();
            if(typeDic.ContainsKey(typeName)) { return; }
            typeDic.Add(typeName, new TypeMemberDic(type));
        });
        return new RoslynTypeInfoStore(typeDic);
    }

    public bool TryGetTypeInfo(string typeName, out TypeMemberDic members)
    {
        return _typeDic.TryGetValue(typeName, out members);
    }

    private static void CollectXmlNodeInstanceTypes<T>(XmlObject xml, T state, CancellationToken ct, Action<T, TypeFullName, CancellationToken> collector)
    {
        CollectRecursively(xml.Root, state, collector, ct);

        static void CollectRecursively(XmlNode node, T state, Action<T, TypeFullName, CancellationToken> collector, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            collector.Invoke(state, node.GetTypeFullName(), ct);

            foreach(var childNode in node.Children) {
                var isPropertyNode = childNode.GetFullName().Name.Split2((byte)'.').Item2.IsEmpty == false;
                if(isPropertyNode) {
                    foreach(var valueNode in childNode.Children) {
                        CollectRecursively(valueNode, state, collector, ct);
                    }
                }
                else {
                    CollectRecursively(childNode, state, collector, ct);
                }
            }
        }
    }
}
