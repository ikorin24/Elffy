#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Elffy.Shading
{
    public abstract class SingleTargetRenderingShader : RenderingShader, ISingleTargetRenderingShader
    {
        private Renderable? _target;

        public Renderable? Target => _target;

        [MemberNotNullWhen(true, nameof(Target))]
        public bool HasTarget => _target is not null;

        protected abstract void OnTargetAttached(Renderable target);

        protected abstract void OnTargetDetached(Renderable detachedTarget);

        protected sealed override void OnAttached(Renderable target)
        {
            base.OnAttached(target);
            if(Interlocked.CompareExchange(ref _target, target, null) != null) {
                throw new InvalidOperationException($"It is not possible to attach to more than one target. {nameof(SingleTargetRenderingShader)} has already a target.");
            }
            _target = target;
            OnTargetAttached(target);
        }

        protected override void OnDetached(Renderable detachedTarget)
        {
            base.OnDetached(detachedTarget);
            var t = Interlocked.CompareExchange(ref _target, null, detachedTarget);
            Debug.Assert(ReferenceEquals(t, detachedTarget));
            OnTargetDetached(detachedTarget);
        }
    }
}
