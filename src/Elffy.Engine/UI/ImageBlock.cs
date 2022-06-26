#nullable enable
using Elffy.Imaging;
using Elffy.Shading;
using Elffy.Markup;
using System;

namespace Elffy.UI
{
    public class ImageBlock : Control
    {
        private ImageBlockContent _source;

        public ImageBlockContent Source { get => _source; set => _source = value; }

        static ImageBlock()
        {
            ControlShaderSelector.SetDefault<ImageBlock>(() => new ImageBlockDefaultShader());
        }

        public ImageBlock()
        {
        }

        private sealed class ImageBlockDefaultShader : ControlDefaultShaderBase
        {
            private ImageBlockContent _current = ImageBlockContent.None;

            public ImageBlockDefaultShader()
            {
            }

            protected override void DefineLocation(VertexDefinition definition, Control target, Type vertexType)
            {
                if(target is not ImageBlock) {
                    throw new InvalidOperationException($"Target must be of type {nameof(ImageBlock)}. (actual: {target.GetType().FullName})");
                }
                base.DefineLocation(definition, target, vertexType);
            }

            protected override void OnRendering(ShaderDataDispatcher dispatcher, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
            {
                var imageBlock = SafeCast.As<ImageBlock>(target);
                var source = imageBlock.Source;
                if(source != _current) {
                    _current = source;
                    source.LoadImage(this, static (image, self) =>
                    {
                        self.ReleaseImage();
                        self.LoadImage(image);
                    });
                }
                base.OnRendering(dispatcher, target, model, view, projection);
            }
        }
    }

    [UseLiteralMarkup]
    [LiteralMarkupPattern(ResourceFile.MarkupPattern, $"new({ResourceFile.MarkupEmit})")]
    public readonly struct ImageBlockContent : IEquatable<ImageBlockContent>
    {
        private readonly ResourceFile _resource;

        public static ImageBlockContent None => default;

        [Obsolete("Don't use default constructor.", true)]
        public ImageBlockContent() => throw new NotSupportedException("Don't use default constructor.");

        public ImageBlockContent(ResourceFile resource)
        {
            _resource = resource;
        }

        public void LoadImage<T>(T state, ReadOnlyImageAction<T> callback)
        {
            using var image = _resource.LoadImage();
            callback.Invoke(image, state);
        }

        public override bool Equals(object? obj) => obj is ImageBlockContent content && Equals(content);

        public bool Equals(ImageBlockContent other) => _resource.Equals(other._resource);

        public override int GetHashCode() => _resource.GetHashCode();

        public static bool operator ==(ImageBlockContent left, ImageBlockContent right) => left.Equals(right);

        public static bool operator !=(ImageBlockContent left, ImageBlockContent right) => !(left == right);
    }
}
