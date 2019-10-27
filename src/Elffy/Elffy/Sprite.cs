using Elffy.Core;
using Elffy.Exceptions;
using Elffy.Threading;
using System;
using System.Threading.Tasks;
using Elffy.Core.MetaFile;

namespace Elffy
{
    /// <summary><see cref="Renderable"/> に適用するスプライトクラス</summary>
    public sealed class Sprite : TextureBase, IDisposable
    {
        private bool _disposed;
        /// <summary>このスプライトが持つテクスチャ</summary>
        private Texture[] _textures;

        /// <summary>このスプライトのページ数を取得します</summary>
        public int PageCount => _textures?.Length ?? 0;

        /// <summary>スプライトのページ切り替えを行うアルゴリズムを取得または設定します</summary>
        public Func<int> PageChangingAlgorithm
        {
            get => _pageChangingAlgorithm;
            set
            {
                ExceptionManager.ThrowIfNullArg(value, nameof(value));
                _pageChangingAlgorithm = value;
            }
        }
        private Func<int> _pageChangingAlgorithm = () => 0;

        #region constructor
        /// <summary>ミップマップありのテクスチャを生成します</summary>
        private Sprite()
            : this(TextureShrinkMode.Bilinear, TextureMipmapMode.Bilinear, TextureExpansionMode.Bilinear) { }

        /// <summary>ミップマップの有無を指定してテクスチャを生成します</summary>
        /// <param name="useMipmap">ミップマップを使用するかどうか</param>
        private Sprite(bool useMipmap)
            : this(TextureShrinkMode.Bilinear, useMipmap ? TextureMipmapMode.Bilinear : TextureMipmapMode.None, TextureExpansionMode.Bilinear) { }

        /// <summary>拡大縮小方法を指定してミップマップなしのテクスチャを生成します</summary>
        /// <param name="shrinkMode">縮小方法</param>
        /// <param name="expansionMode">拡大方法</param>
        private Sprite(TextureShrinkMode shrinkMode, TextureExpansionMode expansionMode)
            : this(shrinkMode, TextureMipmapMode.None, expansionMode) { }

        /// <summary>拡大縮小方法とミップマップのモードを指定してテクスチャを生成します</summary>
        /// <param name="shrinkMode">縮小方法</param>
        /// <param name="mipmapMode">ミップマップのモード</param>
        /// <param name="expansionMode">拡大方法</param>
        private Sprite(TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode, TextureExpansionMode expansionMode)
            : base(shrinkMode, mipmapMode, expansionMode)
        {
        }
        #endregion

        ~Sprite() => Dispose(false);

        /// <summary>リソースからスプライトをロードします</summary>
        /// <param name="resource">リソース名</param>
        /// <returns>ロードしたスプライト</returns>
        public static Sprite LoadFrom(string resource)
        {
            SpriteMetadata spriteInfo;
            using(var stream = Resources.GetStream(resource)) {
                spriteInfo = Metadata.LoadFromStream<SpriteMetadata>(stream);
            }
            var sprite = new Sprite(TextureShrinkMode.Bilinear, TextureMipmapMode.Bilinear, TextureExpansionMode.Bilinear);
            sprite.PixelWidth = spriteInfo.PixelWidth;
            sprite.PixelHeight = spriteInfo.PixelHeight;
            sprite._textures = Texture.LoadFrom(spriteInfo);
            return sprite;
        }

        /// <summary>非同期でリソースからスプライトをロードします</summary>
        /// <param name="resource">リソース名</param>
        /// <returns>ロードしたスプライト</returns>
        public static async Task<Sprite> LoadFromAsync(string resource)
        {
            return await NonCaptureContextTaskFactory.StartNew(() => LoadFrom(resource));
        }

        internal override void SwitchBind()
        {
            var page = PageChangingAlgorithm();
            ExceptionManager.ThrowIf(page < 0 || page >= _textures.Length, new ArgumentOutOfRangeException($"Page number which {nameof(PageChangingAlgorithm)} returns is out of range."));
            unsafe {
                _textures[page].SwitchBind();
            }
        }

        #region Dispose
        /// <summary><see cref="IDisposable.Dispose"/> 実装</summary>
        public void Dispose()
        {
            if(_disposed) { return; }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resource here.
                    foreach(var texture in _textures) {
                        texture.Dispose();
                    }
                    _textures = null;
                }
                // Release unmanaged resource here.

                _disposed = true;
            }
        }
        #endregion
    }
}
