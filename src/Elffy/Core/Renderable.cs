#nullable enable
using OpenTK;
using System;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using Elffy.Threading;
using Elffy.Exceptions;

namespace Elffy.Core
{
    /// <summary>
    /// 画面に描画されるオブジェクトの基底クラス<para/>
    /// 描画に関する操作を提供します<para/>
    /// </summary>
    public abstract class Renderable : Positionable, IDisposable
    {
        private int _indexArrayLength;
        /// <summary>VBOバッファ番号</summary>
        private int _vertexBuffer;
        /// <summary>IBO番号</summary>
        private int _indexBuffer;

        private int _mvp;
        private int _color;

        /// <summary>VAO</summary>
        private int _vao;
        private bool _disposed;
        private bool _isLoaded;

        /// <summary>描画処理を行うかどうか</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>マテリアルを取得または設定します</summary>
        /// <exception cref="ArgumentNullException"></exception>
        public Material Material
        {
            get => _material;
            set
            {
                ArgumentChecker.ThrowIfNullArg(value, nameof(value));
                if(_material == value) { return; }
                var old = _material;
                _material = value;
                MaterialChanged?.Invoke(this, new ValueChangedEventArgs<Material>(old, value));
            }
        }
        private Material _material = Material.Default;

        /// <summary>テクスチャ</summary>
        /// <exception cref="ArgumentNullException"></exception>
        public TextureBase Texture
        {
            get => _texture;
            set
            {
                ArgumentChecker.ThrowIfNullArg(value, nameof(value));
                if(_texture == value) { return; }
                var old = _texture;
                _texture = value;
                TextureChanged?.Invoke(this, new ValueChangedEventArgs<TextureBase>(old, value));
            }
        }
        private TextureBase _texture = TextureBase.Empty;

        /// <summary>シェーダー</summary>
        /// <exception cref="ArgumentNullException"></exception>
        public ShaderProgram Shader
        {
            get => _shader;
            set
            {
                ArgumentChecker.ThrowIfNullArg(value, nameof(value));
                if(_shader == value) { return; }
                var old = _shader;
                _shader = value;
                ShaderChanged?.Invoke(this, new ValueChangedEventArgs<ShaderProgram>(old, value));
            }
        }
        private ShaderProgram _shader = ShaderProgram.Default;

        public Shader S { get; set; }

        /// <summary>頂点色を使用するかどうか</summary>
        public bool EnableVertexColor
        {
            get => _enableVertexColor;
            set
            {
                _enableVertexColor = value;
                if(_vao == Consts.NULL) { return; }
                if(_enableVertexColor) {
                    GL.BindVertexArray(_vao);
                    GL.EnableClientState(ArrayCap.ColorArray);
                }
                else {
                    GL.BindVertexArray(_vao);
                    GL.DisableClientState(ArrayCap.ColorArray);
                }
            }
        }
        private bool _enableVertexColor;

        /// <summary>Material changed event</summary>
        public event ActionEventHandler<Renderable, ValueChangedEventArgs<Material>>? MaterialChanged;
        /// <summary>Texture changed event</summary>
        public event ActionEventHandler<Renderable, ValueChangedEventArgs<TextureBase>>? TextureChanged;
        /// <summary>Shader changed event</summary>
        public event ActionEventHandler<Renderable, ValueChangedEventArgs<ShaderProgram>>? ShaderChanged;
        /// <summary>Before-rendering event</summary>
        protected event ActionEventHandler<Renderable>? Rendering;
        /// <summary>After-rendering event</summary>
        protected event ActionEventHandler<Renderable>? Rendered;

        ~Renderable() => Dispose(false);

        /// <summary>このインスタンスを描画します</summary>
        internal unsafe void Render()
        {
            if(_isLoaded) {
                // 座標と回転を適用
                GL.Translate(Position);
                var rot = Rotation.ToMatrix4();
                GL.MultMatrix((float*)&rot);
                GL.Scale(Scale);
                Rendering?.Invoke(this);

                Material.Apply();
                Texture.Apply();

                //Shader.Apply();

                if(S != null) {
                    GL.BindBuffer(BufferTarget.UniformBuffer, _mvp);
                    var translate = new Matrix4(1, 0, 0, Position.X,
                                                0, 1, 0, Position.Y,
                                                0, 0, 1, Position.Z,
                                                0, 0, 0, 1);
                    var matrix = Rotation.ToMatrix4() * translate * CurrentScreen.Camera.View * CurrentScreen.Camera.Projection;
                    GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, sizeof(float) * 16, (IntPtr)(&matrix));

                    GL.BindBuffer(BufferTarget.UniformBuffer, _color);
                    var color = Mathmatics.Rand.Color4();
                    GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, sizeof(float) * 4, (IntPtr)(&color));

                    GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, _mvp);
                    GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 1, _color);
                    S?.Apply();
                }
                else {
                    GL.UseProgram(0);
                }

                GL.BindVertexArray(_vao);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
                GL.DrawElements(BeginMode.Triangles, _indexArrayLength, DrawElementsType.UnsignedInt, 0);
                Rendered?.Invoke(this);
            }

            if(HasChild) {
                foreach(var child in Children.OfType<Renderable>()) {
                    GL.PushMatrix();
                    child.Render();
                    GL.PopMatrix();
                }
            }
        }

        internal unsafe void Render(in Matrix4 viewMatrix, Matrix4* modelMatrix)
        {
            if(_isLoaded) {
                // 座標と回転を適用

                *modelMatrix = new Matrix4(Scale.X, 0, 0, 0,
                                           0, Scale.Y, 0, 0,
                                           0, 0, Scale.Z, 0,
                                           0, 0, 0, 1) *
                               Rotation.ToMatrix4() *
                               new Matrix4(1, 0, 0, Position.X,
                                           0, 1, 0, Position.Y,
                                           0, 0, 1, Position.Z,
                                           0, 0, 0, 1) *
                               (*modelMatrix);
                GL.MultMatrix((float*)modelMatrix);
                Rendering?.Invoke(this);
                Material.Apply();
                Texture.Apply();
                Shader.Apply();
                GL.BindVertexArray(_vao);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);


                GL.BindBufferBase(BufferRangeTarget.UniformBuffer, index: 5, buffer: _mvp);

                GL.DrawElements(BeginMode.Triangles, _indexArrayLength, DrawElementsType.UnsignedInt, 0);
                Rendered?.Invoke(this);
            }

            if(HasChild) {
                foreach(var child in Children.OfType<Renderable>()) {
                    GL.PushMatrix();
                    child.Render(viewMatrix, modelMatrix);
                    GL.PopMatrix();
                }
            }
        }

        protected unsafe void LoadGraphicBuffer(ReadOnlySpan<Vertex> vertexArray, ReadOnlySpan<int> indexArray)
        {
            CurrentScreen.Dispatcher.ThrowIfNotMainThread();
            fixed(Vertex* va = vertexArray)
            fixed(int* ia = indexArray) {
                if(_isLoaded) {
                    UpdateBuffer((IntPtr)va, vertexArray.Length, (IntPtr)ia, indexArray.Length);
                }
                else {
                    InitBuffer((IntPtr)va, vertexArray.Length, (IntPtr)ia, indexArray.Length);
                }
            }
        }

        /// <summary>描画する3Dモデル(頂点データ)をGPUメモリにロードします</summary>
        /// <param name="vertexArray">頂点配列</param>
        /// <param name="indexArray">頂点インデックス配列</param>
        protected void LoadGraphicBuffer(IntPtr vertexArray, int vertexArrayLength, IntPtr indexArray, int indexArrayLength)
        {
            CurrentScreen.Dispatcher.ThrowIfNotMainThread();
            if(_isLoaded) {
                UpdateBuffer(vertexArray, vertexArrayLength, indexArray, indexArrayLength);
            }
            else {
                InitBuffer(vertexArray, vertexArrayLength, indexArray, indexArrayLength);
            }
        }

        private void InitBuffer(IntPtr vertexArray, int vertexArrayLength, IntPtr indexArray, int indexArrayLength)
        {
            _indexArrayLength = indexArrayLength;
            // 頂点バッファ(VBO)生成
            _vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            int vertexSize = vertexArrayLength * Vertex.Size;
            GL.BufferData(BufferTarget.ArrayBuffer, vertexSize, vertexArray, BufferUsageHint.StaticDraw);

            // 頂点indexバッファ(IBO)生成
            _indexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            int indexSize = indexArrayLength * sizeof(int);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexSize, indexArray, BufferUsageHint.StaticDraw);

            unsafe {
                _mvp = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.UniformBuffer, _mvp);
                var mvp = stackalloc float[16];
                GL.BufferData(BufferTarget.UniformBuffer, sizeof(float) * 16, (IntPtr)mvp, BufferUsageHint.DynamicDraw);

                _color = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.UniformBuffer, _color);
                var color = stackalloc float[4];
                GL.BufferData(BufferTarget.UniformBuffer, sizeof(float) * 4, (IntPtr)color, BufferUsageHint.DynamicDraw);
            }

            // VAO
            _vao = GL.GenVertexArray();
            GL.BindVertexArray(_vao);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.NormalArray);
            if(EnableVertexColor) { GL.EnableClientState(ArrayCap.ColorArray); }
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            Vertex.GLSetStructLayout();                          // 頂点構造体のレイアウトを指定

            _isLoaded = true;
        }

        private void UpdateBuffer(IntPtr vertexArray, int vertexArrayLength, IntPtr indexArray, int indexArrayLength)
        {
            _indexArrayLength = indexArrayLength;
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexArrayLength * Vertex.Size, vertexArray, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indexArrayLength * sizeof(int), indexArray, BufferUsageHint.StaticDraw);
        }

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resource here.
                }

                // Release unmanaged resource
                if(_isLoaded) {
                    // OpenGLのバッファの削除はメインスレッドで行う必要がある
                    var vbo = _vertexBuffer;
                    var ibo = _indexBuffer;
                    var vao = _vao;
                    var uniform = _mvp;
                    var color = _color;
                    CurrentScreen.Dispatcher.Invoke(() => {
                        GL.DeleteBuffer(vbo);
                        GL.DeleteBuffer(ibo);
                        GL.DeleteBuffer(uniform);
                        GL.DeleteBuffer(color);
                        GL.DeleteVertexArray(vao);
                    });
                }
                _disposed = true;
            }
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
