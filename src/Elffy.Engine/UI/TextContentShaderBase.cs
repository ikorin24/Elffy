﻿#nullable enable
using Elffy.Shading;
using SkiaSharp;
using System;
using System.Diagnostics;

namespace Elffy.UI
{
    internal abstract class TextContentShaderBase : ControlDefaultShaderBase
    {
        private ITextContent? _target;
        private EventSubscription<(ITextContent Sender, string PropertyName)> _subscription;
        private bool _requireUpdateTexture;

        protected TextContentShaderBase()
        {
        }

        protected override void DefineLocation(VertexDefinition definition, Control target, Type vertexType)
        {
            if(target is ITextContent textContent) {
                _target = textContent;
                _subscription = textContent.TextContentChanged.Subscribe(x =>
                {
                    // avoid capturing 'this'
                    var shader = SafeCast.As<Control>(x.Sender).Shader;
                    Debug.Assert(shader is not null);
#if DEBUG
                    Debug.Assert(ReferenceEquals(shader, this));
#endif
                    var self = SafeCast.As<TextContentShaderBase>(shader);
                    self._requireUpdateTexture = true;
                });
                _requireUpdateTexture = true;
            }
            base.DefineLocation(definition, target, vertexType);
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            if(ReferenceEquals(target, _target) == false) {
                Debug.Fail("invalid target");
                return;
            }
            if(_requireUpdateTexture) {
                EraseAndDraw(_target);
                _requireUpdateTexture = false;
            }

            base.OnRendering(dispatcher, target, model, view, projection);
        }

        protected override void OnProgramDisposed()
        {
            _subscription.Dispose();
            _target = null;
            base.OnProgramDisposed();
        }

        protected override (HorizontalAlignment HAlignment, VerticalAlignment VAlignment) GetImageAlignment(Control target)
        {
            if(ReferenceEquals(target, _target) == false) {
                Debug.Fail("invalid target");
                return default;
            }
            return (
                HAlignment: _target.TextAlignment,
                VAlignment: _target.VerticalTextAlignment
            );
        }

        private unsafe void EraseAndDraw(ITextContent target)
        {
            // TODO: font
            var fontFamily = FontFamilies.Instance.GetFontFamilyOrDefault(target.FontFamily);
            using var font = fontFamily.SkTypeface != null ? new SKFont(fontFamily.SkTypeface) : new SKFont();
            font.Size = target.FontSize;
            font.Subpixel = true;
            var options = new TextDrawOptions
            {
                Font = font,
                Background = ColorByte.Transparent,
                Foreground = target.Foreground.ToColorByte(),
            };
            using var result = TextDrawer.Draw(target.Text, options);
            ReleaseImage();
            if(result.IsNone == false) {
                LoadImage(result.Image);
            }
        }
    }
}
