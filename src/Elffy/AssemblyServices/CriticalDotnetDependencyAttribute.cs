#nullable enable
using System;

namespace Elffy.AssemblyServices
{
    // この属性はクラスやメソッドなどが特定の .NET のランタイムや BCL に依存していることを示す目印として使用されます。
    // 例えば BCL 内の内部実装 (private や internal な実装) を呼び出している場合などにこの属性を付与してください。
    // つまり、.NET Standard などの API の仕様的にはコンパイルは通るが、動かない可能性があることを明示的に示しています。
    //
    // (例) .Net Standard 2.0 の API でコンパイルは通るが、.NET Framework 4.8 と .NET Core 2.0 の
    //      BCL の内部実装が異なるため Framework 4.8 では動かない可能性がある
    //
    // ・この属性が付与されているものは必ず対象のランタイム環境で単体テストを通してください。
    // ・この属性が付与されているクラスやメソッドが含まれる場合、.Net Standard をターゲットにせず、
    //   特定の対象のランタイム (.NET Core 3.1 など) 向けにコンパイルしてください

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor,
                    AllowMultiple = false, Inherited = false)]
    internal class CriticalDotnetDependencyAttribute : Attribute
    {
    }
}
