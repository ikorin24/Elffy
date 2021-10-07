#nullable enable

namespace Elffy.Features.Internal
{
    internal interface ILayer
    {
        LayerCollection? OwnerCollection { get; }

        LayerTimingPointList TimingPoints { get; }

        /// <summary>Get or set visiblity of the layer</summary>
        bool IsVisible { get; set; }

        /// <summary>Get count of living objects</summary>
        int ObjectCount { get; }

        /// <summary>Add <see cref="FrameObject"/></summary>
        /// <param name="frameObject"><see cref="FrameObject"/> to add to the list</param>
        void AddFrameObject(FrameObject frameObject);

        /// <summary>Remove <see cref="FrameObject"/></summary>
        /// <param name="frameObject"><see cref="FrameObject"/> to remove from the list</param>
        void RemoveFrameObject(FrameObject frameObject);

        /// <summary>Clear all <see cref="FrameObject"/>s in the lists</summary>
        void ClearFrameObject();

        void ApplyAdd();

        void ApplyRemove();

        void EarlyUpdate();

        void Update();

        void LateUpdate();

        void Render(in LayerRenderInfo renderInfo);
    }

    internal readonly struct LayerRenderInfo
    {
        public readonly Matrix4 View;
        public readonly Matrix4 Projection;
        public readonly Matrix4 UIProjection;

        public LayerRenderInfo(in Matrix4 view, in Matrix4 projection, in Matrix4 uiProjection)
        {
            View = view;
            Projection = projection;
            UIProjection = uiProjection;
        }
    }
}
