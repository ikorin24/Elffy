#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elffy.Markup;

namespace Elffy.UI
{
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
    [MarkupAttachedProperty("Column", "${caller}." + nameof(SetColumnAt) + "(${arg0}, ${obj})", new Type[] { typeof(int) })]
    [MarkupAttachedProperty("Row", "${caller}." + nameof(SetRowAt) + "(${arg0}, ${obj})", new Type[] { typeof(int) })]
    public class Grid : Panel
    {
        private static readonly GridChildLayouter _childLayouter = new GridChildLayouter();

        private GridIndexDic? _gridCol;
        private GridIndexDic? _gridRow;
        private GridSplitDefinitionInternal _colDef;
        private GridSplitDefinitionInternal _rowDef;

        private GridIndexDic GridCol => _gridCol ??= GridIndexDic.Create();
        private GridIndexDic GridRow => _gridRow ??= GridIndexDic.Create();

        public ReadOnlySpan<LayoutLength> RowDefinition => _rowDef.GetDefinition();

        public ReadOnlySpan<LayoutLength> ColumnDefinition => _colDef.GetDefinition();

        public void DefineRow(int count, SpanAction<LayoutLength> setter) => _rowDef.SetDefinition(count, setter);

        public void DefineRow<T>(int count, T arg, SpanAction<LayoutLength, T> setter) => _rowDef.SetDefinition(count, arg, setter);

        public unsafe void DefineRow(ReadOnlySpan<LayoutLength> definition)
        {
            fixed(LayoutLength* ptr = definition) {
                _rowDef.SetDefinition(definition.Length, (IntPtr)ptr, static (row, ptr) =>
                {
                    new Span<LayoutLength>((void*)ptr, row.Length).CopyTo(row);
                });
            }
        }

        public void DefineColumn(int count, SpanAction<LayoutLength> setter) => _colDef.SetDefinition(count, setter);

        public void DefineColumn<T>(int count, T arg, SpanAction<LayoutLength, T> setter) => _colDef.SetDefinition(count, arg, setter);

        public unsafe void DefineColumn(ReadOnlySpan<LayoutLength> definition)
        {
            fixed(LayoutLength* ptr = definition) {
                _colDef.SetDefinition(definition.Length, (IntPtr)ptr, static (col, ptr) =>
                {
                    new Span<LayoutLength>((void*)ptr, col.Length).CopyTo(col);
                });
            }
        }

        public void SetColumnAt(int column, Control control)
        {
            ArgumentNullException.ThrowIfNull(control);
            if(column < 0) { ThrowOutOfRange(nameof(column)); }
            GridCol[control] = column;
        }

        public void SetRowAt(int row, Control control)
        {
            ArgumentNullException.ThrowIfNull(control);
            if(row < 0) { ThrowOutOfRange(nameof(row)); }
            GridRow[control] = row;
        }

        public int GetColumnOf(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);
            return _gridCol is null ? 0 :
                   _gridCol.TryGetValue(control, out var column) ? column : 0;
        }

        public int GetRowOf(Control control)
        {
            ArgumentNullException.ThrowIfNull(control);
            return _gridRow is null ? 0 :
                   _gridRow.TryGetValue(control, out var row) ? row : 0;
        }

        protected override void OnLayoutChildreRecursively()
        {
            foreach(var child in Children.AsSpan()) {
                ControlLayoutHelper.LayoutSelf(child, _childLayouter);
                ControlLayoutHelper.LayoutChildrenRecursively(child);
            }
        }

        protected override void OnDead()
        {
            base.OnDead();
            if(_gridCol is not null) {
                GridIndexDic.Return(ref _gridCol);
            }
            if(_gridRow is not null) {
                GridIndexDic.Return(ref _gridRow);
            }
            _colDef.Dispose();
            _rowDef.Dispose();
        }

        private (Vector2 CellSize, Vector2 CellPosOffset) GetCellSizePos(Vector2 parentSize, int col, int row)
        {
            var cellSize = new Vector2(_colDef.EvalSize(parentSize.X, col),
                                       _rowDef.EvalSize(parentSize.Y, row));
            var cellOffsetPos = new Vector2(_colDef.EvalPosition(parentSize.X, col),
                                            _rowDef.EvalPosition(parentSize.Y, row));

            return (cellSize, cellOffsetPos);
        }

        [DoesNotReturn]
        private static void ThrowOutOfRange(string msg) => throw new ArgumentOutOfRangeException(msg);

        private sealed class GridIndexDic : Dictionary<Control, int>
        {
            private const int MaxPooledCount = 128;
            [ThreadStatic]
            private static GridIndexDic? _pooled;
            [ThreadStatic]
            private static int _pooledCount;

            private GridIndexDic? _next;

            private GridIndexDic()
            {
            }

            public static GridIndexDic Create()
            {
                if(_pooled is null) {
                    Debug.Assert(_pooledCount == 0);
                    return new GridIndexDic();
                }
                var ret = _pooled;
                _pooled = ret._next;
                ret._next = null;
                _pooledCount--;
                Debug.Assert(_pooledCount >= 0);
                return ret;
            }

            public static void Return([MaybeNull] ref GridIndexDic dic)
            {
                Debug.Assert(dic is not null);
                Debug.Assert(dic._next is null);
                if(_pooledCount >= MaxPooledCount) {
                    return;
                }
                dic.Clear();
                dic._next = _pooled;
                _pooled = dic;
                dic = null;
                _pooledCount++;
            }
        }

        private sealed class GridChildLayouter : ControlLayouter
        {
            protected override ContentAreaInfo MesureContentArea(Control parent, Control target)
            {
                var grid = (Grid)parent;

                var padding = grid.Padding;
                var gridContentsSize = grid.ActualSize - new Vector2(padding.Left + padding.Right, padding.Top + padding.Bottom);
                var col = grid.GetColumnOf(target);
                var row = grid.GetRowOf(target);
                var (cellSize, cellPos) = grid.GetCellSizePos(gridContentsSize, col, row);
                return new ContentAreaInfo(cellPos, cellSize, LayoutThickness.Zero);
            }
        }
    }
}
