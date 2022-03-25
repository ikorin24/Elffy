#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System;
using System.Threading;
using U8Xml;

namespace Elffy.Generator;

internal sealed class TypeInfoStore : ITypeInfoStore
{
    private readonly Dictionary<string, (TypeInfo Type, Dictionary<string, TypeInfo> Members)> _typeDic;

    private TypeInfoStore(Dictionary<string, (TypeInfo Type, Dictionary<string, TypeInfo> Members)> typeDic)
    {
        _typeDic = typeDic;
    }

    public static TypeInfoStore Create(XmlObject xml, Compilation compilation, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var types = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach(var typeFullName in CollectInstanceTypes(xml)) {
            ct.ThrowIfCancellationRequested();
            var typeSymbol = compilation.GetTypeByMetadataName(typeFullName);
            if(typeSymbol == null) { continue; }
            types.Add(typeSymbol);
            GetInstanceMembers(typeSymbol, types, ct, static (x, types) => types.Add(x.MemberType));
        }

        var typeDic = new Dictionary<string, (TypeInfo Type, Dictionary<string, TypeInfo> Members)>(types.Count);
        foreach(var type in types) {
            var members = new Dictionary<string, TypeInfo>();
            GetInstanceMembers(type, members, ct, static (x, members) =>
            {
                members.Add(x.Member.Name, new TypeInfo(x.MemberType));
            });
            typeDic.Add(type.Name, (Type: new TypeInfo(type), Members: members));
        }
        return new TypeInfoStore(typeDic);
    }

    public bool TryGetTypeInfo(string typeName, out TypeInfo typeInfo, out TypeMemberInfoStore memberInfoStore)
    {
        if(_typeDic.TryGetValue(typeName, out var value) == false) {
            typeInfo = default;
            memberInfoStore = default;
            return false;
        }
        typeInfo = value.Type;
        memberInfoStore = new TypeMemberInfoStore(value.Members);
        return true;
    }

    private static IEnumerable<string> CollectInstanceTypes(XmlObject xml)
    {
        return Array.Empty<string>();   // TODO:
    }

    private static void GetInstanceMembers<T>(INamedTypeSymbol typeSymbol, T arg, CancellationToken ct, Action<(ISymbol Member, INamedTypeSymbol MemberType), T> collector)
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
