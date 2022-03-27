#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System;
using System.Threading;
using U8Xml;
using Elffy.Markup;
using TypeInfo = Elffy.Markup.TypeInfo;

namespace Elffy.Generator;

internal sealed class RoslynTypeInfoStore : ITypeInfoStore
{
    private readonly Dictionary<string, (INamedTypeSymbol Type, TypeMemberDic Members)> _typeDic;

    private RoslynTypeInfoStore(Dictionary<string, (INamedTypeSymbol Type, TypeMemberDic Members)> typeDic)
    {
        _typeDic = typeDic;
    }

    public static RoslynTypeInfoStore Create(XmlObject xml, Compilation compilation, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var typeDic = new Dictionary<string, (INamedTypeSymbol Type, TypeMemberDic Members)>();
        CollectInstanceTypes(xml, (typeDic, compilation), ct, static (x, typeFullName, ct) =>
        {
            var (typeDic, compilation) = x;
            var type = compilation.GetTypeByMetadataName(typeFullName.ToString());
            if(type == null) { return; }
            var typeName = type.Name;
            if(typeDic.ContainsKey(typeName)) { return; }
            var members = new TypeMemberDic();
            CollectInstanceMembers(type, (typeDic, members), ct, static (m, x) =>
            {
                var (typeDic, members) = x;
                members.Add(m.Member.Name, new TypeInfo(m.MemberType));
                var memberTypeName = m.MemberType.Name;
                if(typeDic.ContainsKey(memberTypeName)) { return; }
                typeDic.Add(memberTypeName, (m.MemberType, TypeMemberDic.Null));
            });
            typeDic.Add(typeName, (type, members));
        });

        foreach(var typeName in typeDic.Keys) {
            ct.ThrowIfCancellationRequested();
            var (type, members) = typeDic[typeName];
            if(members.IsNull == false) { continue; }
            members = new TypeMemberDic();
            CollectInstanceMembers(type, members, ct, static (m, members) =>
            {
                members.Add(m.Member.Name, new TypeInfo(m.MemberType));
            });
            typeDic[typeName] = (type, members);
        }
        return new RoslynTypeInfoStore(typeDic);
    }

    public bool TryGetTypeInfo(string typeName, out TypeInfo typeInfo, out TypeMemberDic members)
    {
        if(_typeDic.TryGetValue(typeName, out var value) == false) {
            typeInfo = default;
            members = default;
            return false;
        }
        typeInfo = new TypeInfo(value.Type);
        members = value.Members;
        return true;
    }

    private static void CollectInstanceTypes<T>(XmlObject xml, T state, CancellationToken ct, Action<T, TypeFullName, CancellationToken> collector)
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

    private static void CollectInstanceMembers<T>(INamedTypeSymbol typeSymbol, T arg, CancellationToken ct, Action<(ISymbol Member, INamedTypeSymbol MemberType), T> collector)
    {
        ct.ThrowIfCancellationRequested();
        var kind = typeSymbol.TypeKind;
        if(!(kind is TypeKind.Class or TypeKind.Struct)) {
            return;
        }
        var members = typeSymbol.GetMembers();
        foreach(var m in members) {
            ct.ThrowIfCancellationRequested();
            if((m.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal) && !m.IsStatic) {
                var t = m.Kind switch
                {
                    SymbolKind.Property => (m as IPropertySymbol)?.Type as INamedTypeSymbol,
                    SymbolKind.Field => (m as IFieldSymbol)?.Type as INamedTypeSymbol,
                    _ => null,
                };
                if(t is not null) {
                    collector.Invoke((Member: m, MemberType: t), arg);
                }
            }
        }
    }
}
