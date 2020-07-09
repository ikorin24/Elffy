#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using Elffy.Core;
using Elffy.Exceptions;
using Elffy.Imaging;
using Elffy.OpenGL;
using OpenToolkit.Graphics.OpenGL;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using TKPixelFormat = OpenToolkit.Graphics.OpenGL.PixelFormat;

namespace Elffy.Components
{
    public sealed class Texture : ISingleOwnerComponent, IDisposable
    {
        private TextureObject _to;
        public TextureExpansionMode ExpansionMode { get; }
        public TextureShrinkMode ShrinkMode { get; }
        public TextureMipmapMode MipmapMode { get; }

        public TextureUnitNumber TextureUnit { get; }

        public ComponentOwner? Owner { get; private set; }

        public bool AutoDisposeOnDetached { get; }

        public Texture(TextureExpansionMode expansionMode, TextureShrinkMode shrinkMode, TextureMipmapMode mipmapMode, bool autoDispose = true)
        {
            ExpansionMode = expansionMode;
            ShrinkMode = shrinkMode;
            MipmapMode = mipmapMode;
            TextureUnit = TextureUnitNumber.Unit0;
            AutoDisposeOnDetached = autoDispose;
        }

        ~Texture() => Dispose(false);

        public void Apply()
        {
            TextureObject.Bind(_to, TextureUnit);
        }

        public void Load(Bitmap bitmap)
        {
            if(bitmap is null) { throw new ArgumentNullException(nameof(bitmap)); }
            using(var pixels = bitmap.GetPixels(ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)) {
                Load(pixels.Width, pixels.Height, pixels.Ptr);
            }
        }

        private void Load(int pixelWidth, int pixelHeight, IntPtr ptr)
        {
            if(!_to.IsEmpty) { throw new InvalidOperationException("Texture is already loaded."); }

            _to = TextureObject.Create();
            var unit = TextureUnitNumber.Unit0;
            TextureObject.Bind(_to, unit);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, GetMinParameter(ShrinkMode, MipmapMode));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, GetMagParameter(ExpansionMode));
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, pixelWidth, pixelHeight, 0, TKPixelFormat.Bgra, PixelType.UnsignedByte, ptr);
            if(MipmapMode != TextureMipmapMode.None) {
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            }
            TextureObject.Unbind(unit);
        }

        public void OnAttached(ComponentOwner owner)
        {
            if(Owner is null == false) {
                throw new InvalidOperationException($"{nameof(Texture)} is already attached. Can not have multi {nameof(ComponentOwner)}s.");
            }
            Owner = owner;
            if(AutoDisposeOnDetached) {
                owner.Dead += sender =>
                {
                    Debug.Assert(sender is ComponentOwner);
                    Unsafe.As<ComponentOwner>(sender).RemoveComponent<Texture>();
                };
            }
        }

        public void OnDetached(ComponentOwner owner)
        {
            if(Owner == owner) {
                Owner = null;
                if(AutoDisposeOnDetached) {
                    Dispose();
                }
            }
        }

        public void Dispose()
        {
            if(_to.IsEmpty) { return; }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(_to.IsEmpty) { return; }
            if(disposing) {
                TextureObject.Delete(ref _to);
            }
            else {
                // opengl のバッファは作成したスレッドでしか解放できないため
                // ファイナライザ経由では解放不可
                throw new MemoryLeakException(typeof(Texture));
            }
        }


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
    }
}
