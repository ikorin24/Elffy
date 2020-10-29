#nullable enable

using System;

namespace Elffy.Core
{
    internal interface ILayer
    {
        LayerCollection? OwnerCollection { get; }

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
    }
}
