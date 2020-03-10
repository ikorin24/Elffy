#nullable enable
using Elffy.Core;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using Elffy.Exceptions;
using TKMatrix4 = OpenTK.Matrix4;

namespace Elffy
{
    /// <summary><see cref="FrameObject"/> のレイヤークラス</summary>
    [DebuggerDisplay("Layer: {Name} (ObjectCount = {ObjectCount})", Type = nameof(Layer), TargetTypeName = nameof(Layer))]
    public class Layer : ILayer
    {
        private readonly FrameObjectStore _store = new FrameObjectStore();

        /// <summary>このレイヤーのライティングを有効にするかどうか</summary>
        public bool IsLightingEnabled { get; set; }

        public string Name { get; }

        /// <summary>レイヤー名を指定して <see cref="Layer"/> を作成します</summary>
        /// <param name="name">レイヤー名</param>
        public Layer(string name)
        {
            ArgumentChecker.ThrowIfNullArg(name, nameof(name));
            Name = name;
        }

        /// <summary>現在生きている全オブジェクトの数を取得します</summary>
        public int ObjectCount => _store.ObjectCount;

        /// <summary>指定した<see cref="FrameObject"/>を追加します</summary>
        /// <param name="frameObject">追加するオブジェクト</param>
        public void AddFrameObject(FrameObject frameObject) => _store.AddFrameObject(frameObject);

        /// <summary>指定した<see cref="FrameObject"/>を削除します</summary>
        /// <param name="frameObject">削除するオブジェクト</param>
        /// <returns>削除できたかどうか</returns>
        public void RemoveFrameObject(FrameObject frameObject) => _store.RemoveFrameObject(frameObject);

        /// <summary>オブジェクトの追加と削除の変更を適用します</summary>
        internal void ApplyChanging() => _store.ApplyChanging();

        internal void EarlyUpdate() => _store.EarlyUpdate();

        /// <summary>フレームの更新を行います</summary>
        internal void Update() => _store.Update();

        internal void LateUpdate() => _store.LateUpdate();

        /// <summary>保持している <see cref="FrameObject"/> を全て破棄し、リストをクリアします</summary>
        public void ClearFrameObject() => _store.ClearFrameObject();

        /// <summary>画面への投影行列とカメラ行列を指定して、描画を実行します</summary>
        /// <param name="projection">投影行列</param>
        /// <param name="view">カメラ行列</param>
        internal void Render(TKMatrix4 projection, TKMatrix4 view)
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
            GL.MatrixMode(MatrixMode.Modelview);
            foreach(var renderable in _store.Renderables.Where(x => x.IsRoot && x.IsVisible)) {
                GL.LoadMatrix(ref view);
                renderable.Render();
            }

            //GL.MatrixMode(MatrixMode.Projection);
            //GL.LoadMatrix(ref projection);
            //GL.MatrixMode(MatrixMode.Modelview);
            //foreach(var renderable in _store.Renderables.Where(x => x.IsRoot && x.IsVisible)) {
            //    GL.LoadIdentity();
            //    renderable.Render();
            //    GL.MultMatrix(ref view);
            //}
        }
    }
}
