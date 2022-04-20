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
        new[]
        {
            $@"(?<n>{RegexPatterns.Int})(,\s*)?",
            $@"(?<n>{RegexPatterns.Float})\*(,\s*)?",
            $@"\*(,\s*)?",
            @"^",
            @"$",
        },
        new[]
        {
            @"new global::Elffy.UI.LayoutLength((int)(${n}), global::Elffy.UI.LayoutLengthType.Length), ",
            @"new global::Elffy.UI.LayoutLength((float)(${n}), global::Elffy.UI.LayoutLengthType.Proportion), ",
            @"new global::Elffy.UI.LayoutLength(1f, global::Elffy.UI.LayoutLengthType.Proportion), ",
            @"${obj}.DefineColumn(stackalloc new global::Elffy.UI.LayoutLength[] { ",
            @" });",
        })
    ]
    // (ex)
    // <Grid RowDefinition="10,0.5*,0.5*,10" />
    [MarkupMemberSetter(
        "RowDefinition",
        $@"^{LayoutLength.MatchPattern}(,\s*{LayoutLength.MatchPattern})*$",
        new[]
        {
            $@"(?<n>{RegexPatterns.Int})(,\s*)?",
            $@"(?<n>{RegexPatterns.Float})\*(,\s*)?",
            $@"\*(,\s*)?",
            @"^",
            @"$",
        },
        new[]
        {
            @"new global::Elffy.UI.LayoutLength((int)(${n}), global::Elffy.UI.LayoutLengthType.Length), ",
            @"new global::Elffy.UI.LayoutLength((float)(${n}), global::Elffy.UI.LayoutLengthType.Proportion), ",
            @"new global::Elffy.UI.LayoutLength(1f, global::Elffy.UI.LayoutLengthType.Proportion), ",
            @"${obj}.DefineRow(stackalloc new global::Elffy.UI.LayoutLength[] { ",
            @" });",
        })
    ]
    // (ex)
    // <Grid RowDefinition="0.5*,0.5*" ColumnDefinition="0.5*,0.5*">
    //   <Button Grid.Row="0" Grid.Column="0"/>
    // </Grid>
    [MarkupAttachedProperty("Column", "${caller}." + nameof(SetColumnAt) + "(${arg0}, ${obj})", new Type[] { typeof(int) })]
    [MarkupAttachedProperty("Row", "${caller}." + nameof(SetRowAt) + "(${arg0}, ${obj})", new Type[] { typeof(int) })]
    partial class Grid
    {
    }
}
