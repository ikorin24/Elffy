#nullable enable
using System;
using U8Xml;
using System.ComponentModel;
using System.Text;
using System.Diagnostics;

namespace Elffy.Generator;

[DebuggerDisplay("{ToString()}")]
internal readonly struct TypeFullName : IEquatable<TypeFullName>
{
    private readonly RawString _namespaceName;
    private readonly RawString _name;

    public RawString NamespaceName => _namespaceName;
    public RawString Name => _name;
    public bool HasNamespace => _namespaceName.IsEmpty == false;

    [Obsolete("Don't use default constructor.", true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public TypeFullName() => throw new NotSupportedException("Don't use default constructor.");

    public TypeFullName(RawString namespaceName, RawString name)
    {
        _namespaceName = namespaceName;
        _name = name;
    }

    public override unsafe string ToString()
    {
        if(HasNamespace == false) {
            return _name.ToString();
        }
        var utf8 = Encoding.UTF8;
        var nsCharLen = utf8.GetCharCount((byte*)_namespaceName.Ptr, _namespaceName.Length);
        var nCharLen = utf8.GetCharCount((byte*)_name.Ptr, _name.Length);
        var charLen = nsCharLen + nCharLen + 1;

        var state = (Self: this, NsCharLen: nsCharLen, NCharLen: nCharLen);

        var str = new string('\0', charLen);
        var nsName = state.Self.NamespaceName;
        var name = state.Self.Name;
        fixed(char* buf = str) {
            utf8.GetChars((byte*)nsName.Ptr, nsName.Length, buf, state.NsCharLen);
            utf8.GetChars((byte*)name.Ptr, name.Length, buf + nsName.Length + 1, state.NCharLen);
            buf[nsName.Length] = '.';
        }
        return str;
    }

    public override bool Equals(object? obj) => obj is TypeFullName name && Equals(name);

    public bool Equals(TypeFullName other) => _namespaceName == other._namespaceName && _name == other._name;

    public override int GetHashCode()
    {
        int hashCode = -92357541;
        hashCode = hashCode * -1521134295 + _namespaceName.GetHashCode();
        hashCode = hashCode * -1521134295 + _name.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(TypeFullName left, TypeFullName right) => left.Equals(right);

    public static bool operator !=(TypeFullName left, TypeFullName right) => !(left == right);
}
