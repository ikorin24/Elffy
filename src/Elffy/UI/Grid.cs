#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI
{
    public class Grid : Panel
    {
        private GridIndexDic? _gridCol;
        private GridIndexDic? _gridRow;
        private GridSplitDefinition _colDef;
        private GridSplitDefinition _rowDef;

        private GridIndexDic GridCol => _gridCol ??= GridIndexDic.Create();
        private GridIndexDic GridRow => _gridRow ??= GridIndexDic.Create();

        public void RowDefinition(ReadOnlySpan<LayoutLength> definition)
        {
            _rowDef.SetDefinition(definition);
        }

        public void ColumnDefinition(ReadOnlySpan<LayoutLength> definition)
        {
            _colDef.SetDefinition(definition);
        }

        internal void SetGridColumnOf(Control control, int column)
        {
            if(control is null) { ThrowNullArg(nameof(control)); }
            if(column < 0) { ThrowOutOfRange(nameof(column)); }
            GridCol[control] = column;
        }

        internal int GetGridColumnOf(Control control)
        {
            if(control is null) { ThrowNullArg(nameof(control)); }
            return _gridCol is null ? 0 :
                   _gridCol.TryGetValue(control, out var column) ? column : 0;
        }

        internal void SetGridRowOf(Control control, int row)
        {
            if(control is null) { ThrowNullArg(nameof(control)); }
            if(row < 0) { ThrowOutOfRange(nameof(row)); }
            GridRow[control] = row;
        }

        internal int GetGridRowOf(Control control)
        {
            if(control is null) { ThrowNullArg(nameof(control)); }
            return _gridRow is null ? 0 :
                   _gridRow.TryGetValue(control, out var row) ? row : 0;
        }

        //protected override void LayoutRecursively(in Vector2 parentSize, in LayoutThickness parentPadding)
        //{
        //    LayoutSelf(parentSize, parentPadding, out var size);

        //    //_rowDef.EvalSize
        //    foreach(var child in Children.AsSpan()) {
        //        var col = child.GetGridColumn(this);
        //        var row = child.GetGridRow(this);

        //        var childSize = new Vector2(_colDef.EvalSize(parentSize.X, col),
        //                                    _rowDef.EvalSize(parentSize.Y, row));
        //        var childPos = new Vector2(_colDef.EvalPosition(parentSize.X, col),
        //                                   _rowDef.EvalPosition(parentSize.Y, row));
        //        // TODO: どうしよ…
        //    }
        //}

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

        [DoesNotReturn]
        private static void ThrowNullArg(string msg) => throw new ArgumentNullException(msg);

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
    }

    public static class GridIndexExtension
    {
        public static void SetGridColumn(this Control control, Grid targetGrid, int column)
        {
            targetGrid.SetGridColumnOf(control, column);
        }

        public static int GetGridColumn(this Control control, Grid targetGrid)
        {
            return targetGrid.GetGridColumnOf(control);
        }

        public static void SetGridRow(this Control control, Grid targetGrid, int row)
        {
            targetGrid.SetGridRowOf(control, row);
        }

        public static int GetGridRow(this Control control, Grid targetGrid)
        {
            return targetGrid.GetGridRowOf(control);
        }
    }
}
