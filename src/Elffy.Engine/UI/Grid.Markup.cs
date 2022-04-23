#nullable enable
using System;
using Elffy.Markup;

namespace Elffy.UI
{
    // (ex)
    // <Grid ColumnDefinition="10,0.5*,0.5*,10" />
    [MarkupMemberSetter(
        "ColumnDefinition",
        $@"^{LayoutLength.MatchPattern}(,\s*{LayoutLength.MatchPattern})*$",
        new string[5]
        {
            $@"(?<n>{RegexPatterns.Int})(,\s*)?",
            $@"(?<n>{RegexPatterns.Float})\*(,\s*)?",
            $@"\*(,\s*)?",
            @"^",
            @"$",
        },
        new string[5]
        {
            @"new global::Elffy.UI.LayoutLength((int)(${n}), global::Elffy.UI.LayoutLengthType.Length), ",
            @"new global::Elffy.UI.LayoutLength((float)(${n}), global::Elffy.UI.LayoutLengthType.Proportion), ",
            @"new global::Elffy.UI.LayoutLength(1f, global::Elffy.UI.LayoutLengthType.Proportion), ",
            @$"${{obj}}.{nameof(DefineColumn)}((global::System.ReadOnlySpan<global::Elffy.UI.LayoutLength>)stackalloc[] {{ ",
            @" });",
        })
    ]
    // (ex)
    // <Grid RowDefinition="10,0.5*,0.5*,10" />
    [MarkupMemberSetter(
        "RowDefinition",
        $@"^{LayoutLength.MatchPattern}(,\s*{LayoutLength.MatchPattern})*$",
        new string[5]
        {
            $@"(?<n>{RegexPatterns.Int})(,\s*)?",
            $@"(?<n>{RegexPatterns.Float})\*(,\s*)?",
            $@"\*(,\s*)?",
            @"^",
            @"$",
        },
        new string[5]
        {
            @"new global::Elffy.UI.LayoutLength((int)(${n}), global::Elffy.UI.LayoutLengthType.Length), ",
            @"new global::Elffy.UI.LayoutLength((float)(${n}), global::Elffy.UI.LayoutLengthType.Proportion), ",
            @"new global::Elffy.UI.LayoutLength(1f, global::Elffy.UI.LayoutLengthType.Proportion), ",
            $@"${{obj}}.{nameof(DefineRow)}((global::System.ReadOnlySpan<global::Elffy.UI.LayoutLength>)stackalloc[] {{ ",
            @" });",
        })
    ]
    // (ex)
    // <Button Grid.Row="0" Grid.Column="0"/>
    [MarkupAttachedMember("Column", "${caller}." + nameof(SetColumnAt) + "($_, ${obj})", typeof(int))]
    [MarkupAttachedMember("Row", "${caller}." + nameof(SetRowAt) + "($_, ${obj})", typeof(int))]
    partial class Grid
    {
    }
}
