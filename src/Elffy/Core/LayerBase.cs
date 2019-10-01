using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Core
{
    /// <summary><see cref="FrameObject"/> を所持するレイヤーの機能を提供するクラス</summary>
    public abstract class LayerBase
    {
        /// <summary>レイヤーの名前</summary>
        public string Name { get; set; }

        /// <summary>このレイヤーが持つ <see cref="FrameObject"/> を保持しておくためのオブジェクト</summary>
        internal FrameObjectStore ObjectStore { get; private set; } = new FrameObjectStore();

        /// <summary>このレイヤーに <see cref="FrameObject"/> を追加します</summary>
        /// <param name="frameObject">追加する <see cref="FrameObject"/></param>
        internal void AddFrameObject(FrameObject frameObject) => ObjectStore.AddFrameObject(frameObject);

        /// <summary>このレイヤーから <see cref="FrameObject"/> を削除します</summary>
        /// <param name="frameObject">削除する <see cref="FrameObject"/></param>
        /// <returns></returns>
        internal void RemoveFrameObject(FrameObject frameObject) => ObjectStore.RemoveFrameObject(frameObject);

        /// <summary><see cref="FrameObject"/> のタグを使って、このレイヤーに含まれる <see cref="FrameObject"/> を探します (存在しない場合 null を返します)</summary>
        /// <param name="tag"><see cref="FrameObject"/> のタグ</param>
        /// <returns>検索で得られた <see cref="FrameObject"/></returns>
        public FrameObject FindObject(string tag) => ObjectStore.FindObject(tag);

        /// <summary><see cref="FrameObject"/> のタグを使って、このレイヤーに含まれる <see cref="FrameObject"/> を全て探します (存在しない場合、要素数0のリストを返します)</summary>
        /// <param name="tag"><see cref="FrameObject"/> のタグ</param>
        /// <returns>検索で得られた <see cref="FrameObject"/></returns>
        public List<FrameObject> FindAllObject(string tag) => ObjectStore.FindAllObject(tag);

        /// <summary>このレイヤーが持つ <see cref="FrameObject"/> を全て破棄します</summary>
        public void ClearFrameObject() => ObjectStore.ClearFrameObject();
    }
}
