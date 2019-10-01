using Elffy.Threading;
using Elffy.UI;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core
{
    /// <summary><see cref="FrameObject"/> を保持しておくためのクラスです。</summary>
    internal class FrameObjectStore
    {
        #region private member
        /// <summary>現在生きている全オブジェクトのリスト</summary>
        private readonly List<FrameObject> _list = new List<FrameObject>();
        /// <summary>このフレームで追加されたオブジェクトのリスト (次のフレームの最初に <see cref="_list"/> に追加されます)</summary>
        private readonly List<FrameObject> _addedBuf = new List<FrameObject>();
        /// <summary>このフレームで削除されたオブジェクトのリスト (次のフレームの最初に <see cref="_list"/> から削除されます)</summary>
        private readonly List<FrameObject> _removedBuf = new List<FrameObject>();
        /// <summary><see cref="_list"/> に含まれるオブジェクトのうち、<see cref="Renderable"/> を継承しているもののリスト</summary>
        private readonly List<Renderable> _renderables = new List<Renderable>();
        private readonly List<IUIRenderable> _uiList = new List<IUIRenderable>();
        private readonly List<IUIRenderable> _addedUIBuf = new List<IUIRenderable>();
        private readonly List<IUIRenderable> _removedUIBuf = new List<IUIRenderable>();
        #endregion

        #region AddFrameObject
        /// <summary>指定した<see cref="FrameObject"/>を追加します</summary>
        /// <param name="frameObject">追加するオブジェクト</param>
        public void AddFrameObject(FrameObject frameObject)
        {
            if(frameObject == null) { throw new ArgumentNullException(nameof(frameObject)); }
            if(frameObject is Renderable renderable) {
                switch(renderable.RenderingLayer) {
                    case RenderingLayer.World:
                        _addedBuf.Add(renderable);
                        break;
                    case RenderingLayer.UI:
                        if(renderable is IUIRenderable ui) {
                            _addedUIBuf.Add(ui);
                        }
                        break;
                    default:
                        break;
                }
            }
            else {
                _addedBuf.Add(frameObject);
            }
        }
        #endregion

        #region RemoveFrameObject
        /// <summary>指定した<see cref="FrameObject"/>を削除します</summary>
        /// <param name="frameObject">削除するオブジェクト</param>
        /// <returns>削除できたかどうか</returns>
        public bool RemoveFrameObject(FrameObject frameObject)
        {
            if(frameObject == null) { throw new ArgumentNullException(nameof(frameObject)); }
            if(frameObject is Renderable renderable) {
                switch(renderable.RenderingLayer) {
                    case RenderingLayer.World:
                        _removedBuf.Add(frameObject);
                        break;
                    case RenderingLayer.UI:
                        _removedUIBuf.Add((IUIRenderable)frameObject);
                        if(renderable is IUIRenderable ui) {
                            _removedUIBuf.Add(ui);
                        }
                        break;
                    default:
                        break;
                }
            }
            else {
                _removedBuf.Remove(frameObject);
            }
            return true;
        }
        #endregion

        #region ApplyChanging
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
            if(_removedUIBuf.Count > 0) {
                foreach(var item in _removedUIBuf) {
                    _uiList.Remove(item);
                }
                _removedUIBuf.Clear();
            }
            if(_addedUIBuf.Count > 0) {
                _uiList.AddRange(_addedUIBuf);
                _addedUIBuf.Clear();
            }
        }
        #endregion

        #region FindObject
        /// <summary>タグを指定して <see cref="FrameObject"/> を取得します (指定されたオブジェクトが見つからない場合 null を返します)</summary>
        /// <param name="tag">検索する <see cref="FrameObject"/> のタグ</param>
        /// <returns>検索で得られた <see cref="FrameObject"/></returns>
        public FrameObject FindObject(string tag)
        {
            return _list.Find(x => x.Tag == tag);
        }
        #endregion

        #region FindAllObject
        /// <summary>指定されたタグを持つ <see cref="FrameObject"/> を全て取得します (見つからない場合、要素数0のリストを返します)</summary>
        /// <param name="tag">検索する <see cref="FrameObject"/> のタグ</param>
        /// <returns>検索で得られた <see cref="FrameObject"/> のリスト</returns>
        public List<FrameObject> FindAllObject(string tag)
        {
            return _list.FindAll(x => x.Tag == tag) ?? new List<FrameObject>();
        }
        #endregion

        #region Update
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
        #endregion

        #region Render
        /// <summary>画面への投影行列とカメラ行列を指定して、描画を実行します</summary>
        /// <param name="projection">投影行列</param>
        /// <param name="view">カメラ行列</param>
        public void Render(Matrix4 projection, Matrix4 view)
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref view);
            // TODO: OpenGL の行列スタックの深さを確認
            foreach(var renderable in _renderables.Where(x => x.IsVisible)) {
                GL.PushMatrix();
                renderable.Render();
                GL.PopMatrix();
            }
        }
        #endregion

        #region RenderUI
        /// <summary>画面への投影行列を指定して、描画を実行します</summary>
        /// <param name="projection">投影行列</param>
        public void RenderUI(Matrix4 projection)
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            // TODO: OpenGL の行列スタックの深さを確認
            foreach(var uiRenderable in _uiList.Where(r => r.IsVisible)) {
                GL.PushMatrix();
                uiRenderable.Render();
                GL.PopMatrix();
            }
        }
        #endregion

        #region Clear
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
            _uiList.Clear();
            _addedUIBuf.Clear();
            _removedUIBuf.Clear();
        }
        #endregion
    }
}
