#nullable enable

using System;

namespace Elffy.Core
{
    internal interface ILayer
    {
        LayerCollection? OwnerCollection { get; }

        ReadOnlySpan<Light> Lights { get; }

        /// <summary>現在生きている全オブジェクトの数を取得します</summary>
        int ObjectCount { get; }

        /// <summary>指定した<see cref="FrameObject"/>を追加します</summary>
        /// <param name="frameObject">追加するオブジェクト</param>
        void AddFrameObject(FrameObject frameObject);

        /// <summary>指定した<see cref="FrameObject"/>を削除します</summary>
        /// <param name="frameObject">削除するオブジェクト</param>
        /// <returns>削除できたかどうか</returns>
        void RemoveFrameObject(FrameObject frameObject);

        /// <summary>保持している <see cref="FrameObject"/> を全て破棄し、リストをクリアします</summary>
        void ClearFrameObject();
    }
}
