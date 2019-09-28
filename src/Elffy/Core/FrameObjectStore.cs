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
    internal class FrameObjectStore
    {
        #region private member
        private readonly List<FrameObject> _list = new List<FrameObject>();
        private readonly List<FrameObject> _addedBuf = new List<FrameObject>();
        private readonly List<FrameObject> _removedBuf = new List<FrameObject>();
        private readonly List<Renderable> _renderables = new List<Renderable>();
        private readonly List<IUIRenderable> _uiList = new List<IUIRenderable>();
        private readonly List<IUIRenderable> _addedUIBuf = new List<IUIRenderable>();
        private readonly List<IUIRenderable> _removedUIBuf = new List<IUIRenderable>();
        #endregion

        #region AddFrameObject
        public bool AddFrameObject(FrameObject frameObject)
        {
            if(frameObject == null) { return false; }
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
            return true;
        }
        #endregion

        #region RemoveFrameObject
        public bool RemoveFrameObject(FrameObject frameObject)
        {
            if(frameObject == null) { return false; }
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
        public FrameObject FindObject(string tag)
        {
            return _list.Find(x => x.Tag == tag);
        }
        #endregion

        #region FindAllObject
        public List<FrameObject> FindAllObject(string tag)
        {
            return _list.FindAll(x => x.Tag == tag) ?? new List<FrameObject>();
        }
        #endregion

        #region Update
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
        public void Render(Matrix4 projection, Matrix4 view)
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref view);
            foreach(var renderable in _renderables.Where(x => x.IsVisible)) {
                GL.PushMatrix();
                renderable.Render();
                GL.PopMatrix();
            }
        }
        #endregion

        #region RenderUI
        public void RenderUI(Matrix4 projection)
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            foreach(var uiRenderable in _uiList.Where(r => r.IsVisible)) {
                GL.PushMatrix();
                uiRenderable.Render();
                GL.PopMatrix();
            }
        }
        #endregion

        #region Clear
        public void Clear()
        {
            foreach(var item in _list) {
                item.Destroy();
            }
            _list.Clear();
            _addedBuf.Clear();
            _removedBuf.Clear();
        }
        #endregion
    }
}
