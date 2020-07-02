#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elffy.Core;
using Elffy.Exceptions;
using Elffy.UI;
using Elffy.Effective;
using Elffy.Effective.Unsafes;

namespace Elffy
{
    /// <summary><see cref="Layer"/>のリストを表すクラスです。</summary>
    [DebuggerTypeProxy(typeof(LayerCollectionDebuggerTypeProxy))]
    [DebuggerDisplay("LayerCollection (Count = {Count})")]
    public class LayerCollection : IReadOnlyList<Layer>, IReadOnlyCollection<Layer>, ICollection<Layer>
    {
        private const string WORLD_LAYER_NAME = "World";
        private readonly List<Layer> _list = new List<Layer>();

        internal RenderingArea OwnerRenderingArea { get; }

        /// <summary>UI レイヤーを取得します (このレイヤーはリストには含まれません。インスタンスを public にも公開しないでください)</summary>
        internal UILayer UILayer { get; }

        /// <summary>ワールドレイヤーを取得します</summary>
        public Layer WorldLayer { get; }

        /// <summary>システムレイヤー (このレイヤーはリストには含まれません。インスタンスを public にも公開しないでください)</summary>
        internal SystemLayer SystemLayer { get; }

        /// <summary>レイヤーの数を取得します</summary>
        public int Count => _list.Count;

        bool ICollection<Layer>.IsReadOnly => false;

        /// <summary>インデックスを指定して、レイヤーを取得、設定します</summary>
        /// <param name="index">インデックス</param>
        /// <returns>レイヤー</returns>
        public Layer this[int index]
        {
            get
            {
                ArgumentChecker.ThrowOutOfRangeIf((uint)index >= (uint)_list.Count, nameof(index), index, "value is out of range");
                return _list[index];
            }
        }
        
        internal LayerCollection(RenderingArea owner)
        {
            OwnerRenderingArea = owner;
            UILayer = new UILayer(this);
            SystemLayer = new SystemLayer(this);
            WorldLayer = new Layer(WORLD_LAYER_NAME, this);
            AddDefaltLayers();
        }

        /// <summary>
        /// レイヤーを追加します<para/>
        /// </summary>
        /// <param name="layer">追加する要素</param>
        public void Add(Layer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            if(layer.Owner != null) { throw new InvalidOperationException($"指定のレイヤーは既に別の {nameof(LayerCollection)} に含まれています。"); }
            layer.Owner = this;
            _list.Add(layer);
        }

        /// <summary>レイヤーをクリアします (デフォルトのレイヤーはクリアされません)</summary>
        public void Clear()
        {
            foreach(var layer in _list.AsSpan()) {
                layer.Owner = null;
            }
            _list.Clear();
            AddDefaltLayers();
        }

        /// <summary>リスト中にレイヤーが含まれているかを取得します</summary>
        /// <param name="layer">確認するレイヤー</param>
        /// <returns>リスト中に指定レイヤーが含まれているか</returns>
        public bool Contains(Layer layer) => _list.Contains(layer);

        /// <summary>リストのレイヤーを配列にコピーします</summary>
        /// <param name="array">コピー先の配列</param>
        /// <param name="arrayIndex">コピー先の配列のコピーを開始するインデックス</param>
        public void CopyTo(Layer[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        /// <summary>レイヤーをリストから削除します</summary>
        /// <param name="layer">削除するレイヤー</param>
        /// <returns>削除に成功したか (指定した要素が存在しない場合 false)</returns>
        public bool Remove(Layer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            var removed = _list.Remove(layer);
            if(removed) {
                layer.Owner = null;
            }
            return removed;
        }

        internal ReadOnlySpan<Layer> AsReadOnlySpan() => _list.AsReadOnlySpan();

        public List<Layer>.Enumerator GetEnumerator() => _list.GetEnumerator();

        IEnumerator<Layer> IEnumerable<Layer>.GetEnumerator() => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();


        private void AddDefaltLayers()
        {
            _list.Add(WorldLayer);
        }
    }

    #region class LayerCollectionDebuggerTypeProxy
    internal class LayerCollectionDebuggerTypeProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private LayerCollection _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Layer[] Layers
        {
            get
            {
                var layers = new Layer[_entity.Count];
                _entity.CopyTo(layers, 0);
                return layers;
            }
        }

        public LayerCollectionDebuggerTypeProxy(LayerCollection entity) => _entity = entity;
    }
    #endregion class LayerCollectionDebuggerTypeProxy<T>
}
