﻿#nullable enable
using Elffy.Core;
using Elffy.Effective;
using Elffy.Imaging;
using Elffy.OpenGL;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Components
{
    public class MultiTexture : ISingleOwnerComponent, IDisposable
    {
        private SingleOwnerComponentCore _core;             // Mutable object, Don't change into readonly
        private ValueTypeRentMemory<TextureCore> _textureCores;
        private bool _isLoaded;

        public bool IsEmpty => _isLoaded == false;

        public ComponentOwner? Owner => _core.Owner;

        public bool AutoDisposeOnDetached => _core.AutoDisposeOnDetached;

        public int Count => _textureCores.Length;

        public MultiTexture()
        {
        }

        ~MultiTexture() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public (Vector2i size, TextureObject texture) GetTextureInfo(int index)
        {
            ref var t = ref _textureCores[index];
            return (size: t.Size, texture: t.Texture);
        }

        public unsafe void Load(ReadOnlySpan<Image> images)
        {
            if(images.IsEmpty) { return; }

            var texCores = new ValueTypeRentMemory<TextureCore>(images.Length, true);
            var span = texCores.Span;
            try {
                for(int i = 0; i < span.Length; i++) {
                    var core = new TextureCore();
                    span[i] = core;
                    core.Load(images[i]);
                }
                ContextAssociatedMemorySafety.Register(this, Engine.CurrentContext!);
                _textureCores = texCores;
            }
            catch {
                DisposeTextures(ref texCores);
                throw;
            }
        }

        public MultiTextureLoaderContext GetLoaderContext(int count)
        {
            return new MultiTextureLoaderContext(this, count);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing) {
                _isLoaded = false;
                DisposeTextures(ref _textureCores);
            }
            else {
                ContextAssociatedMemorySafety.OnFinalized(this);
            }
        }

        public virtual void OnAttached(ComponentOwner owner) => _core.OnAttached(owner, this);

        public virtual void OnDetached(ComponentOwner owner) => _core.OnDetached(owner, this);

        private static void DisposeTextures(ref ValueTypeRentMemory<TextureCore> cores)
        {
            foreach(var core in cores.Span) {
                core.Dispose();
            }
            cores.Dispose();
        }

        internal void InitializeCapacity(int count)
        {
            if(_isLoaded) {
                throw new InvalidOperationException("Already loaded. Can not change the image");
            }
            _textureCores = new ValueTypeRentMemory<TextureCore>(count);
        }

        internal void LoadImage(int index, in ReadOnlyImageRef image, in TextureConfig config)
        {
            if(_isLoaded) {
                throw new InvalidOperationException("Already loaded. Can not change the image");
            }
            if((uint)index >= (uint)_textureCores.Length) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            ref var core = ref Unsafe.NullRef<TextureCore>();
            try {
                core = ref _textureCores[index];
                if(core.IsEmpty == false) {
                    core.Dispose();
                }
                core = new TextureCore(config);
                core.Load(image);
            }
            catch {
                if(Unsafe.IsNullRef(ref core)) {
                    core.Dispose();
                }
                throw;
            }
        }

        internal void EndLoading()
        {
            _isLoaded = true;
        }
    }

    public readonly struct MultiTextureLoaderContext : IDisposable
    {
        private readonly MultiTexture? _multiTexture;

        public int TextureCount => _multiTexture?.Count ?? 0;

        internal MultiTextureLoaderContext(MultiTexture multiTexture, int count)
        {
            _multiTexture = multiTexture;
            multiTexture.InitializeCapacity(count);
            ContextAssociatedMemorySafety.Register(multiTexture, Engine.CurrentContext!);
        }

        public void Load(int index, in ReadOnlyImageRef image)
        {
            Load(index, image, TextureConfig.Default);
        }

        public void Load(int index, in ReadOnlyImageRef image, in TextureConfig config)
        {
            _multiTexture?.LoadImage(index, image, config);
        }


        public void Dispose()
        {
            _multiTexture?.EndLoading();
        }
    }
}