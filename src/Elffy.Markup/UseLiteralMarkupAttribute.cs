#nullable enable
using System;

namespace Elffy.Markup;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class UseLiteralMarkupAttribute : Attribute
{
}
