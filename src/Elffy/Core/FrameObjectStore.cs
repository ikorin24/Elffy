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
        private readonly List<FrameObject> _list = new List<FrameObject>();
        private readonly List<FrameObject> _addedBuf = new List<FrameObject>();
        private readonly List<FrameObject> _removedBuf = new List<FrameObject>();

        #region AddFrameObject
        public bool AddFrameObject(FrameObject frameObject)
        {
            if(frameObject == null) { return false; }
            if(frameObject is Renderable renderable) {
                if(renderable.RenderingLayer == RenderingLayer.World) {
                    _addedBuf.Add(renderable);
                }
            }
            return true;
        }
        #endregion

        #region RemoveFrameObject
        public bool RemoveFrameObject(FrameObject frameObject)
        {
            if(frameObject == null) { return false; }
            if(frameObject is Renderable renderable) {
                if(renderable.RenderingLayer == RenderingLayer.World) {
                    _removedBuf.Add(frameObject);
                }
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
                }
            }
            if(_addedBuf.Count > 0) {
                _list.AddRange(_addedBuf);
                _addedBuf.Clear();
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

        public void Render()
        {
            var renderables = _list.OfType<Renderable>().Where(x => x.IsRoot);
            GL.MatrixMode(MatrixMode.Projection);
            var projection = Camera.Current.Projection;
            GL.LoadMatrix(ref projection);
            GL.MatrixMode(MatrixMode.Modelview);
            var viewMatrix = Camera.Current.Matrix;
            GL.LoadMatrix(ref viewMatrix);
            foreach(var frameObject in renderables.Where(x => x.IsVisible)) {
                GL.PushMatrix();
                frameObject.Render();
                GL.PopMatrix();
            }
        }

        public void Clear()
        {
            foreach(var item in _list) {
                item.Destroy();
            }
            _list.Clear();
            _addedBuf.Clear();
            _removedBuf.Clear();
        }
    }
}
