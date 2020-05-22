#nullable enable
using System;
using OpenTK.Graphics.OpenGL;
using Elffy.OpenGL;
using Elffy.Components;

namespace Elffy.Core
{
    public abstract class MultiPartsRenderable : Renderable
    {
        private RenderableParts[]? _parts;
        private MultiTexture? _textures;
        private MultiTexture? Textures => _textures ?? 
            (TryGetComponent<IComponentInternal<MultiTexture>>(out var textures) ? (_textures = textures.Self) : _textures);

        public MultiPartsRenderable()
        {
            ComponentDetached += (sender, e) => { if(e == _textures) { _textures = null; } };
        }

        protected void SetParts(RenderableParts[] parts)
        {
            _parts = parts ?? throw new ArgumentNullException(nameof(parts));
        }

        protected override void OnRendering()
        {
            var parts = _parts;
            if(parts != null) {
                var pos = 0;
                var textures = Textures;
                foreach(var p in parts) {
                    textures?.Apply(p.TextureIndex);
                    GL.DrawElements(BeginMode.Triangles, p.VertexCount, DrawElementsType.UnsignedInt, pos * sizeof(int));
                    pos += p.VertexCount;
                }
            }
        }

        public readonly struct RenderableParts
        {
            public readonly int VertexCount;
            public readonly int TextureIndex;

            public RenderableParts(int vertexCount, int textureIndex)
            {
                VertexCount = vertexCount;
                TextureIndex = textureIndex;
            }
        }
    }
}
