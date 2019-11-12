using Elffy.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.UI
{
    public class ControlCollection : IReadOnlyList<Control>, IReadOnlyCollection<Control>, ICollection<Control>, IEnumerable<Control>, IEnumerable
    {
        /// <summary>この <see cref="ControlCollection"/> インスタンスを持つ <see cref="Control"/> オブジェクト</summary>
        private Control _owner;
        private List<Control> _list;

        /// <summary>インデックスを指定してリストの要素にアクセスします</summary>
        /// <param name="index">インデックス</param>
        /// <returns>指定した要素</returns>
        public Control this[int index] => _list[index];

        /// <summary>リストの要素数</summary>
        public int Count => _list.Count;

        /// <summary><see cref="ICollection{T}.IsReadOnly"/> 実装。常に false を返します。</summary>
        public bool IsReadOnly => false;

        /// <summary>コンストラクタ</summary>
        /// <param name="owner">この <see cref="ControlCollection"/> を持つ <see cref="Control"/> オブジェクト</param>
        internal ControlCollection(Control owner)
        {
            ArgumentChecker.ThrowIfNullArg(owner, nameof(owner));
            _owner = owner;
            _list = new List<Control>();
        }

        /// <summary>
        /// 要素を追加します<para/>
        /// ※パフォーマンスのため <see cref="Control"/> の親子関係は循環を検知しません。ツリーの循環は予期せぬ例外や無限ループに陥る可能性があります。
        /// </summary>
        /// <param name="item">追加する要素</param>
        public void Add(Control item)
        {
            ArgumentChecker.ThrowIfNullArg(item, nameof(item));
            item.Parent = _owner;
            item.Renderable.Activate();
            _list.Add(item);
        }

        /// <summary>要素を複数追加します</summary>
        /// <param name="items">追加する要素</param>
        public void AddRange(IEnumerable<Control> items)
        {
            ArgumentChecker.ThrowIfNullArg(items, nameof(items));
            var evaluated = (items is ICollection) ? items : items.ToArray();
            ArgumentChecker.ThrowArgumentIf(evaluated.Contains(null), $"{nameof(items)} contains null. Can not add null");
            foreach(var item in evaluated) {
                item.Parent = _owner;
                item.Renderable.Activate();
            }
            _list.AddRange(evaluated);
        }

        /// <summary>要素をクリアします</summary>
        public void Clear()
        {
            _list.ForEach(x =>
            {
                x.Parent = null;
                x.Renderable.Destroy();
            });
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

        /// <summary>列挙子を取得します</summary>
        /// <returns>列挙子</returns>
        public IEnumerator<Control> GetEnumerator() => _list.GetEnumerator();

        /// <summary>指定要素のインデックスを取得します</summary>
        /// <param name="item">インデックスを取得する要素</param>
        /// <returns>要素のインデックス</returns>
        public int IndexOf(Control item) => _list.IndexOf(item);

        /// <summary>インデックスを指定して要素を追加します</summary>
        /// <param name="index">インデックス</param>
        /// <param name="item">追加する要素</param>
        public void Insert(int index, Control item)
        {
            ArgumentChecker.ThrowIfNullArg(item, nameof(item));
            ArgumentChecker.ThrowOutOfRangeIf(index < 0 || index > _list.Count, nameof(index), index, $"{nameof(index)} is out of range");
            item.Parent = _owner;
            _list.Insert(index, item);
            item.Renderable.Activate();
        }

        /// <summary>要素をリストから削除します</summary>
        /// <param name="item">削除する要素</param>
        /// <returns>削除に成功したか (指定した要素が存在しない場合 false)</returns>
        public bool Remove(Control item)
        {
            ArgumentChecker.ThrowIfNullArg(item, nameof(item));
            var result = _list.Remove(item);
            if(result) {
                item.Parent = null;
                item.Renderable.Destroy();
            }
            return result;
        }

        /// <summary>指定のインデックスの要素を削除します</summary>
        /// <param name="index">インデックス</param>
        public void RemoveAt(int index)
        {
            ArgumentChecker.ThrowOutOfRangeIf(index < 0 || index >= _list.Count, nameof(index), index, $"{nameof(index)} is out of range");
            _list[index].Parent = null;
            _list[index].Renderable.Destroy();
            _list.RemoveAt(index);
        }

        /// <summary>列挙子を取得します</summary>
        /// <returns>列挙子</returns>
        IEnumerator IEnumerable.GetEnumerator() => (_list as IEnumerable).GetEnumerator();
    }
}
