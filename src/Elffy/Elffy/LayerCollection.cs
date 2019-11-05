using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy.Core;
using Elffy.Exceptions;

namespace Elffy
{
    /// <summary><see cref="Layer"/>のリストを表すクラスです。</summary>
    [DebuggerTypeProxy(typeof(LayerCollectionDebuggerTypeProxy))]
    [DebuggerDisplay("LayerCollection (Count = {Count})")]
    public class LayerCollection : IList<Layer>, IReadOnlyList<Layer>, IReadOnlyCollection<Layer>, ICollection<Layer>, IEnumerable<Layer>, IEnumerable
    {
        private const string UI_LAYER_NAME = "UILayer";
        private const string WORLD_LAYER_NAME = "WorldLayer";
        private const string SYSTEM_LAYER_NAME = "SystemLayer";
        private readonly List<Layer> _list = new List<Layer>();

        /// <summary>UI レイヤーを取得します</summary>
        public Layer UILayer { get; } = new UILayer(UI_LAYER_NAME);

        /// <summary>ワールドレイヤーを取得します</summary>
        public Layer WorldLayer { get; } = new Layer(WORLD_LAYER_NAME) { IsLightingEnabled = true };

        /// <summary>システムレイヤー (このレイヤーはリストには含まれません。インスタンスを public にも公開しないでください)</summary>
        internal InternaInvisiblelLayer SystemLayer { get; } = new InternaInvisiblelLayer(SYSTEM_LAYER_NAME);

        /// <summary>レイヤーの数を取得します</summary>
        public int Count => _list.Count;

        /// <summary>このリストが読み取り専用かどうかを取得します。常に false を返します。</summary>
        public bool IsReadOnly => false;

        /// <summary>インデックスを指定して、レイヤーを取得、設定します</summary>
        /// <param name="index">インデックス</param>
        /// <returns>レイヤー</returns>
        public Layer this[int index]
        {
            get
            {
                ArgumentChecker.ThrowIf(index < 0 || index > _list.Count - 1, new ArgumentOutOfRangeException(nameof(index), index, "value is out of range."));
                return _list[index];
            }
            set
            {
                ArgumentChecker.ThrowIfNullArg(value, nameof(value));
                _list[index] = value;
            }
        }

        internal LayerCollection()
        {
            AddDefaltLayers();
        }

        /// <summary>システム用のレイヤーを含めて、全レイヤーを取得します</summary>
        /// <returns>全レイヤー</returns>
        internal IEnumerable<LayerBase> GetAllLayer()
        {
            yield return SystemLayer;
            foreach(var layer in _list) {
                yield return layer;
            }
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

        /// <summary>レイヤーを複数追加します</summary>
        /// <param name="layers">追加するレイヤー</param>
        public void AddRange(IEnumerable<Layer> layers)
        {
            ArgumentChecker.ThrowIfNullArg(layers, nameof(layers));
            var evaluated = (layers is ICollection) ? layers : layers.ToArray();
            ArgumentChecker.ThrowIf(evaluated.Contains(null), new ArgumentException($"{layers} contain 'null'. Can not add null."));
            _list.AddRange(evaluated);
        }

        /// <summary>レイヤーをクリアします (デフォルトのレイヤーはクリアされません)</summary>
        public void Clear()
        {
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

        /// <summary>列挙子を取得します</summary>
        /// <returns>列挙子</returns>
        public IEnumerator<Layer> GetEnumerator() => _list.GetEnumerator();

        /// <summary>指定レイヤーのインデックスを取得します</summary>
        /// <param name="layer">インデックスを取得するレイヤー</param>
        /// <returns>レイヤーのインデックス</returns>
        public int IndexOf(Layer layer) => _list.IndexOf(layer);

        /// <summary>インデックスを指定してレイヤーを追加します</summary>
        /// <param name="index">インデックス</param>
        /// <param name="layer">追加するレイヤー</param>
        public void Insert(int index, Layer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            ArgumentChecker.ThrowIf(index < 0 || index > _list.Count, new ArgumentOutOfRangeException(nameof(index), index, "value is out of range."));
            _list.Insert(index, layer);
        }

        /// <summary>レイヤーをリストから削除します</summary>
        /// <param name="layer">削除するレイヤー</param>
        /// <returns>削除に成功したか (指定した要素が存在しない場合 false)</returns>
        public bool Remove(Layer layer)
        {
            ArgumentChecker.ThrowIfNullArg(layer, nameof(layer));
            return _list.Remove(layer);
        }

        /// <summary>指定のインデックスのレイヤーを削除します</summary>
        /// <param name="index">インデックス</param>
        public void RemoveAt(int index)
        {
            ArgumentChecker.ThrowIf(index < 0 || index >= _list.Count, new ArgumentOutOfRangeException(nameof(index)));
            _list.RemoveAt(index);
        }

        /// <summary>列挙子を取得します</summary>
        /// <returns>列挙子</returns>
        IEnumerator IEnumerable.GetEnumerator() => (_list as IEnumerable).GetEnumerator();

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
