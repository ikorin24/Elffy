#nullable enable
using Elffy.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Elffy.Core
{
    /// <summary><see cref="FrameObject"/> を保持しておくためのクラスです。</summary>
    internal class FrameObjectStore
    {
        /// <summary>現在生きている全オブジェクトのリスト</summary>
        private readonly List<FrameObject> _list = new List<FrameObject>();
        /// <summary>このフレームで追加されたオブジェクトのリスト (次のフレームの最初に <see cref="_list"/> に追加されます)</summary>
        private readonly List<FrameObject> _addedBuf = new List<FrameObject>();
        /// <summary>このフレームで削除されたオブジェクトのリスト (次のフレームの最初に <see cref="_list"/> から削除されます)</summary>
        private readonly List<FrameObject> _removedBuf = new List<FrameObject>();
        /// <summary><see cref="_list"/> に含まれるオブジェクトのうち、<see cref="Renderable"/> を継承しているもののリスト</summary>
        private readonly List<Renderable> _renderables = new List<Renderable>();

        /// <summary>現在生きている全オブジェクトを取得します</summary>
        public IEnumerable<FrameObject> List => _list;
        /// <summary>現在生きている全オブジェクトの数を取得します</summary>
        public int ObjectCount => _list.Count;

        /// <summary>現在のフレームで追加されたオブジェクトを取得します</summary>
        public IEnumerable<FrameObject> Added => _addedBuf;

        /// <summary>現在のフレームで削除されたオブジェクトを取得します</summary>
        public IEnumerable<FrameObject> Removed => _removedBuf;

        /// <summary><see cref="List"/> に含まれるオブジェクトのうち、<see cref="Renderable"/> を継承しているものを取得します</summary>
        public IEnumerable<Renderable> Renderables => _renderables;

        /// <summary>指定した<see cref="FrameObject"/>を追加します</summary>
        /// <param name="frameObject">追加するオブジェクト</param>
        public void AddFrameObject(FrameObject frameObject)
        {
            ArgumentChecker.ThrowIfNullArg(frameObject, nameof(frameObject));
            _addedBuf.Add(frameObject);
        }

        /// <summary>指定した<see cref="FrameObject"/>を削除します</summary>
        /// <param name="frameObject">削除するオブジェクト</param>
        /// <returns>削除できたかどうか</returns>
        public void RemoveFrameObject(FrameObject frameObject)
        {
            ArgumentChecker.ThrowIfNullArg(frameObject, nameof(frameObject));
            _removedBuf.Add(frameObject);
        }

        /// <summary>オブジェクトの追加と削除の変更を適用します</summary>
        public void ApplyChanging()
        {
            if(_removedBuf.Count > 0) {
                foreach(var item in _removedBuf) {
                    _list.Remove(item);
                    if(item is Renderable renderable) {
                        _renderables.Remove(renderable);
                    }
                }
                _removedBuf.Clear();
            }
            if(_addedBuf.Count > 0) {
                _list.AddRange(_addedBuf);
                _renderables.AddRange(_addedBuf.OfType<Renderable>());
                _addedBuf.Clear();
            }
        }

        /// <summary>フレームの更新を行います</summary>
        public void Update()
        {
            foreach(var frameObject in _list.Where(x => !x.IsFrozen)) {
                if(frameObject.IsStarted == false) {
                    frameObject.Start();
                    frameObject.IsStarted = true;
                }
                frameObject.Update();
            }
        }

        /// <summary>保持している <see cref="FrameObject"/> を全て破棄し、リストをクリアします</summary>
        public void ClearFrameObject()
        {
            _addedBuf.Clear();          // 追加オブジェクトのリストを先にクリア
            foreach(var item in _list) {
                item.Destroy();         // 生きているオブジェクトをすべて破棄
            }
            ApplyChanging();            // 変更を全て適用

            // 全リストをクリア
            _list.Clear();
            _removedBuf.Clear();
            _renderables.Clear();
        }
    }
}
