using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using Elffy.Core;
using System.Drawing;
using Elffy.Threading;
using Elffy.Effective;
using System.Diagnostics;

namespace Elffy.Core
{
    /// <summary><see cref="Renderable"/> が持つテクスチャの基底クラス</summary>
    public abstract class TextureBase
    {
        private static int _current;

        protected const int BYTE_PER_PIXEL = 4;
        /// <summary>ピクセル配列のピクセルの</summary>
        protected const PixelFormat PIXEL_FORMAT = PixelFormat.Bgra;

        /// <summary>テクスチャの縮小モードを取得します</summary>
        public TextureShrinkMode ShrinkMode { get; }
        /// <summary>テクスチャの拡大モードを取得します</summary>
        public TextureExpansionMode ExpansionMode { get; }
        /// <summary>テクスチャのミップマップモードを取得します</summary>
        public TextureMipmapMode MipmapMode { get; }
        /// <summary>テクスチャがミップマップを持っているかどうかを取得します</summary>
        public bool HasMipmap => MipmapMode != TextureMipmapMode.None;
        /// <summary>テクスチャのピクセル幅を取得します</summary>
        public int PixelWidth { get; protected set; }
        /// <summary>テクスチャのピクセル高さを取得します</summary>
        public int PixelHeight { get; protected set; }

        protected internal abstract int TextureID { get; }

        /// <summary>空のテクスチャを取得します</summary>
        internal static TextureBase Empty => _empty ?? (_empty = new EmptyTexture());
        private static TextureBase _empty;

        /// <summary>コンストラクタ</summary>
        /// <param name="shrinkMode">テクスチャの縮小モード</param>
        /// <param name="mipmapMode">テクスチャのミップマップモード</param>
        /// <param name="expansionMode">テクスチャの拡大モード</param>
        public TextureBase(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode, TextureExpansionMode expansionMode)
        {
            ShrinkMode = shrinkMode;
            MipmapMode = mipmapMode;
            ExpansionMode = expansionMode;
        }

        internal virtual void Apply()
        {
            ChangeTextureBind(TextureID);
        }

        /// <summary>バッファに Texture を読み込みます</summary>
        /// <param name="textureBuffer">OpenGL のテクスチャのバッファ</param>
        /// <param name="shrinkMode">縮小方法</param>
        /// <param name="mipmapMode">ミップマップのモード</param>
        /// <param name="expansionMode">拡大方法</param>
        /// <param name="pixels">テクスチャのピクセル配列</param>
        /// <param name="pixelWidth">テクスチャのピクセル幅</param>
        /// <param name="pixelHeight">テクスチャのピクセル高</param>
        protected void SetTexture(int textureBuffer, TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode, TextureExpansionMode expansionMode, IntPtr pixels, int pixelWidth, int pixelHeight)
        {
            ChangeTextureBind(textureBuffer);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, GetMinParameter(shrinkMode, mipmapMode));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, GetMagParameter(expansionMode));
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, pixelWidth, pixelHeight, 0, PIXEL_FORMAT, PixelType.UnsignedByte, pixels);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        /// <summary>OpenGL に設定するテクスチャの拡大のパラメータを取得します</summary>
        /// <param name="expansionMode">拡大のモード</param>
        /// <returns>OpenGL に設定するテクスチャの拡大のパラメータ</returns>
        private int GetMagParameter(TextureExpansionMode expansionMode)
        {
            switch(expansionMode) {
                case TextureExpansionMode.Bilinear:
                    return (int)TextureMagFilter.Linear;
                case TextureExpansionMode.NearestNeighbor:
                    return (int)TextureMagFilter.Nearest;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>OpenGL に設定するテクスチャの縮小とミップマップのパラメータを取得します</summary>
        /// <param name="shrinkMode">縮小のモード</param>
        /// <param name="mipmapMode">ミップマップのモード</param>
        /// <returns>OpenGL に設定するテクスチャの縮小とミップマップのパラメータ</returns>
        private int GetMinParameter(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode)
        {
            switch(shrinkMode) {
                case TextureShrinkMode.Bilinear:
                    switch(mipmapMode) {
                        case TextureMipmapMode.None:
                            return (int)TextureMinFilter.Linear;
                        case TextureMipmapMode.Bilinear:
                            return (int)TextureMinFilter.LinearMipmapLinear;
                        case TextureMipmapMode.NearestNeighbor:
                            return (int)TextureMinFilter.LinearMipmapNearest;
                    }
                    break;
                case TextureShrinkMode.NearestNeighbor:
                    switch(mipmapMode) {
                        case TextureMipmapMode.None:
                            return (int)TextureMinFilter.Nearest;
                        case TextureMipmapMode.Bilinear:
                            return (int)TextureMinFilter.NearestMipmapLinear;
                        case TextureMipmapMode.NearestNeighbor:
                            return (int)TextureMinFilter.NearestMipmapNearest;
                    }
                    break;
            }
            throw new ArgumentException();
        }

        private void ChangeTextureBind(int buffer)
        {
            if(_current != buffer) {
                _current = buffer;
                GL.BindTexture(TextureTarget.Texture2D, _current);
            }
        }


        /// <summary>空のテクスチャを表すテクスチャクラス</summary>
        [DebuggerDisplay("EmptyTexture")]
        private sealed class EmptyTexture : TextureBase
        {
            internal EmptyTexture()
                : base(TextureShrinkMode.NearestNeighbor, TextureMipmapMode.NearestNeighbor, TextureExpansionMode.NearestNeighbor)
            {
            }

            protected internal override int TextureID => Consts.NULL;
        }
    }
}
