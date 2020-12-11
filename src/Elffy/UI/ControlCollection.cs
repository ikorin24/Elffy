#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI
{
    public class ControlCollection : IReadOnlyList<Control>, IReadOnlyCollection<Control>, ICollection<Control>, IEnumerable<Control>, IEnumerable
    {
        /// <summary>この <see cref="ControlCollection"/> インスタンスを持つ <see cref="Control"/> オブジェクト</summary>
        private readonly Control _owner;
        private readonly List<Control> _list;

        /// <summary>インデックスを指定してリストの要素にアクセスします</summary>
        /// <param name="index">インデックス</param>
        /// <returns>指定した要素</returns>
        public Control this[int index] => _list[index];

        /// <summary>リストの要素数</summary>
        public int Count => _list.Count;

        /// <summary><see cref="ICollection{T}.IsReadOnly"/> 実装。常に false を返します。</summary>
        bool ICollection<Control>.IsReadOnly => false;

        /// <summary>コンストラクタ</summary>
        /// <param name="owner">この <see cref="ControlCollection"/> を持つ <see cref="Control"/> オブジェクト</param>
        internal ControlCollection(Control owner)
        {
            Debug.Assert(owner is null == false);
            _owner = owner;
            _list = new List<Control>();
        }

        /// <summary>
        /// 要素を追加します
        /// </summary>
        /// <param name="item">追加する要素</param>
        public void Add(Control item)
        {
            if(item is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(item));
            }
            if(item.LifeState != LifeState.New) { ThrowNotNewControl(); }
            item.Parent = _owner;
            item.Renderable.Activate(item.Root!.UILayer);
            _list.Add(item);
        }

        /// <summary>要素を複数追加します</summary>
        /// <param name="items">追加する要素</param>
        public void AddRange(IEnumerable<Control> items)
        {
            if(items is null) { throw new ArgumentNullException(nameof(items)); }
            foreach(var item in items) {
                if(item is null) { throw new ArgumentException($"{nameof(items)} contains null. Can not add null."); }
                if(item.LifeState != LifeState.New) { ThrowNotNewControl(); }
                item.Parent = _owner;
                _list.Add(item);
                item.Renderable.Activate(item.Root!.UILayer);
            }
        }

        /// <summary>要素を複数追加します</summary>
        /// <param name="items">追加する要素</param>
        public void AddRange(ReadOnlySpan<Control> items)
        {
            foreach(var item in items) {
                if(item.LifeState != LifeState.New) { ThrowNotNewControl(); }
                item.Parent = _owner;
                _list.Add(item);
                item.Renderable.Activate(item.Root!.UILayer);
            }
        }

        /// <summary>要素をクリアします</summary>
        public void Clear()
        {
            foreach(var item in _list.AsReadOnlySpan()) {
                item.Parent = null;
                item.Renderable.Terminate();
            }
            _list.Clear();
        }

        /// <summary>リスト中に要素が含まれているかを取得します</summary>
        /// <param name="item">確認する要素</param>
        /// <returns>リスト中に指定要素が含まれているか</returns>
        public bool Contains(Control item) => _list.Contains(item);

        /// <summary>リストの要素を配列にコピーします</summary>
        /// <param name="array">コピー先の配列</param>
        /// <param name="arrayIndex">コピー先の配列のコピーを開始するインデックス</param>
        public void CopyTo(Control[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        /// <summary>指定要素のインデックスを取得します</summary>
        /// <param name="item">インデックスを取得する要素</param>
        /// <returns>要素のインデックス</returns>
        public int IndexOf(Control item) => _list.IndexOf(item);

        /// <summary>インデックスを指定して要素を追加します</summary>
        /// <param name="index">インデックス</param>
        /// <param name="item">追加する要素</param>
        public void Insert(int index, Control item)
        {
            if(item is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(item));
            }
            if(index < 0 || index > _list.Count) {
                ThrowOutOfRange(index);
                [DoesNotReturn] static void ThrowOutOfRange(int value) => throw new ArgumentOutOfRangeException(nameof(index), value, $"{nameof(index)} is out of range.");
            }
            if(item.LifeState == LifeState.New) { ThrowNotNewControl(); }
            item.Parent = _owner;
            _list.Insert(index, item);
            item.Renderable.Activate(item.Root!.UILayer);
        }

        /// <summary>要素をリストから削除します</summary>
        /// <param name="item">削除する要素</param>
        /// <returns>削除に成功したか (指定した要素が存在しない場合 false)</returns>
        public bool Remove(Control item)
        {
            if(item is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(item));
            }
            var result = _list.Remove(item);
            if(result) {
                item.Parent = null;
                item.Renderable.Terminate();
            }
            return result;
        }

        /// <summary>指定のインデックスの要素を削除します</summary>
        /// <param name="index">インデックス</param>
        public void RemoveAt(int index)
        {
            if((uint)index >= (uint)_list.Count) {
                ThrowOutOfRange(index);
                [DoesNotReturn] static void ThrowOutOfRange(int value) => throw new ArgumentOutOfRangeException(nameof(index), value, $"{nameof(index)} is out of range.");
            }
            var removed = _list[index];
            _list.RemoveAt(index);
            removed.Parent = null;
            removed.Renderable.Terminate();
        }

        [DoesNotReturn]
        private static void ThrowNotNewControl()
        {
            throw new ArgumentException($"{nameof(Control)} object is not new.");
        }

        internal ReadOnlySpan<Control> AsReadOnlySpan() => _list.AsReadOnlySpan();

        /// <summary>列挙子を取得します</summary>
        /// <returns>列挙子</returns>
        public List<Control>.Enumerator GetEnumerator() => _list.GetEnumerator();

        /// <summary>列挙子を取得します</summary>
        /// <returns>列挙子</returns>
        IEnumerator<Control> IEnumerable<Control>.GetEnumerator() => _list.GetEnumerator();

        /// <summary>列挙子を取得します</summary>
        /// <returns>列挙子</returns>
        IEnumerator IEnumerable.GetEnumerator() => (_list as IEnumerable).GetEnumerator();
    }
}
