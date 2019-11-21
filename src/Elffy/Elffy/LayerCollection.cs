#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Elffy.Core;
using Elffy.Exceptions;

namespace Elffy
{
    /// <summary><see cref="Layer"/>のリストを表すクラスです。</summary>
    [DebuggerTypeProxy(typeof(LayerCollectionDebuggerTypeProxy))]
    [DebuggerDisplay("LayerCollection (Count = {Count})")]
    public class LayerCollection : IList<Layer>, IReadOnlyList<Layer>, IReadOnlyCollection<Layer>, IList
    {
        private const string UI_LAYER_NAME = "UILayer";
        private const string WORLD_LAYER_NAME = "WorldLayer";
        private const string SYSTEM_LAYER_NAME = "SystemLayer";
        private readonly List<Layer> _list = new List<Layer>();

        /// <summary>UI レイヤーを取得します</summary>
        internal UILayer UILayer { get; } = new UILayer(UI_LAYER_NAME);

        /// <summary>ワールドレイヤーを取得します</summary>
        public Layer WorldLayer { get; } = new Layer(WORLD_LAYER_NAME) { IsLightingEnabled = true };

        /// <summary>システムレイヤー (このレイヤーはリストには含まれません。インスタンスを public にも公開しないでください)</summary>
        internal InternaInvisiblelLayer SystemLayer { get; } = new InternaInvisiblelLayer(SYSTEM_LAYER_NAME);

        /// <summary>レイヤーの数を取得します</summary>
        public int Count => _list.Count;

        /// <summary>このリストが読み取り専用かどうかを取得します。常に false を返します。</summary>
        public bool IsReadOnly => false;
        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        object ICollection.SyncRoot
        {
            get
            {
                if(_syncRoot == null) {
                    Interlocked.CompareExchange(ref _syncRoot, new object(), null!);
                }
                return _syncRoot;
            }
        }
        private object? _syncRoot;

        bool ICollection.IsSynchronized => false;

        /// <summary>インデックスを指定して、レイヤーを取得、設定します</summary>
        /// <param name="index">インデックス</param>
        /// <returns>レイヤー</returns>
        public Layer this[int index]
        {
            get
            {
                ArgumentChecker.ThrowOutOfRangeIf(index < 0 || index > _list.Count - 1, nameof(index), index, "value is out of range");
                return _list[index];
            }
            set
            {
                ArgumentChecker.ThrowIfNullArg(value, nameof(value));
                _list[index] = value;
            }
        }
        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (Layer)value;
        }

        internal LayerCollection()
        {
            AddDefaltLayers();
        }

        /// <summary>
        /// レイヤーを追加します<para/>
        /// </summary>
        /// <param name="layer">追加する要素</param>
        public void Add(Layer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            _list.Add(layer);
        }

        int IList.Add(object value)
        {
            Add((Layer)value);
            return Count - 1;
        }


        /// <summary>レイヤーを複数追加します</summary>
        /// <param name="layers">追加するレイヤー</param>
        public void AddRange(IEnumerable<Layer> layers)
        {
            ArgumentChecker.ThrowIfNullArg(layers, nameof(layers));
            var evaluated = (layers is ICollection) ? layers : layers.ToArray();
            ArgumentChecker.ThrowArgumentIf(evaluated.Contains(null!), $"{nameof(layers)} contain 'null'. Can not add null.");
            _list.AddRange(evaluated);
        }

        /// <summary>レイヤーをクリアします (デフォルトのレイヤーはクリアされません)</summary>
        public void Clear()
        {
            _list.Clear();
            AddDefaltLayers();
        }

        void IList.Clear() => Clear();

        /// <summary>リスト中にレイヤーが含まれているかを取得します</summary>
        /// <param name="layer">確認するレイヤー</param>
        /// <returns>リスト中に指定レイヤーが含まれているか</returns>
        public bool Contains(Layer layer) => _list.Contains(layer);

        bool IList.Contains(object value) => Contains((Layer)value);

        /// <summary>リストのレイヤーを配列にコピーします</summary>
        /// <param name="array">コピー先の配列</param>
        /// <param name="arrayIndex">コピー先の配列のコピーを開始するインデックス</param>
        public void CopyTo(Layer[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        void ICollection.CopyTo(Array array, int index)
        {
            if(array is Layer[] layers) {
                _list.CopyTo(layers, index);
            }
        }

        /// <summary>指定レイヤーのインデックスを取得します</summary>
        /// <param name="layer">インデックスを取得するレイヤー</param>
        /// <returns>レイヤーのインデックス</returns>
        public int IndexOf(Layer layer) => _list.IndexOf(layer);
        int IList.IndexOf(object value) => (value is Layer layer) ? IndexOf(layer) : -1;

        /// <summary>インデックスを指定してレイヤーを追加します</summary>
        /// <param name="index">インデックス</param>
        /// <param name="layer">追加するレイヤー</param>
        public void Insert(int index, Layer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            ArgumentChecker.ThrowOutOfRangeIf(index < 0 || index > _list.Count, nameof(index), index, "value is out of range.");
            _list.Insert(index, layer);
        }

        void IList.Insert(int index, object value) => Insert(index, (Layer)value);

        /// <summary>レイヤーをリストから削除します</summary>
        /// <param name="layer">削除するレイヤー</param>
        /// <returns>削除に成功したか (指定した要素が存在しない場合 false)</returns>
        public bool Remove(Layer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            return _list.Remove(layer);
        }
        void IList.Remove(object value) => Remove((Layer)value);

        /// <summary>指定のインデックスのレイヤーを削除します</summary>
        /// <param name="index">インデックス</param>
        public void RemoveAt(int index)
        {
            ArgumentChecker.ThrowOutOfRangeIf(index < 0 || index >= _list.Count, nameof(index), index, $"{nameof(index)} is out of range");
            _list.RemoveAt(index);
        }

        void IList.RemoveAt(int index) => RemoveAt(index);

        public List<Layer>.Enumerator GetEnumerator() => _list.GetEnumerator();

        IEnumerator<Layer> IEnumerable<Layer>.GetEnumerator() => _list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();


        private void AddDefaltLayers()
        {
            _list.Add(UILayer);
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
